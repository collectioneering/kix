using System.Security.Cryptography;
using Art;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("arc", HelpText = "Execute archival artifact tools.")]
internal class RunArc : BRunTool, IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = "kix_data.db")]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option('h', "hash", HelpText = "Checksum algorithm (e.g. SHA1|SHA256|SHA384|SHA512|MD5).", Default = "SHA256")]
    public string Hash { get; set; } = null!;

    [Value(0, HelpText = "Profile file.", MetaValue = "file", MetaName = "profileFile", Required = true)]
    public IReadOnlyCollection<string> ProfileFiles { get; set; } = null!;

    [Option("validate", HelpText = "Validate resources with known checksums and re-obtain with provided profiles.")]
    public bool Validate { get; set; }

    [Option("validate-only", HelpText = "Validate resources with known checksums but do not re-obtain.")]
    public bool ValidateOnly { get; set; }

    [Option("add-checksum", HelpText = "Add checksum to resources without checksum during validation.")]
    public bool AddChecksum { get; set; }

    [Option('u', "update",
        HelpText = $"Resource update mode ({nameof(ResourceUpdateMode.ArtifactSoft)}|[{nameof(ResourceUpdateMode.ArtifactHard)}]|{nameof(ResourceUpdateMode.Soft)}|{nameof(ResourceUpdateMode.Hard)}).",
        MetaValue = "mode", Default = ResourceUpdateMode.ArtifactHard)]
    public ResourceUpdateMode Update { get; set; }

    [Option('f', "full", HelpText = "Ignore nonfull artifacts.")]
    public bool Full { get; set; }

    [Option('s', "skip", HelpText = $"Skip artifacts ([{nameof(ArtifactSkipMode.None)}]|{nameof(ArtifactSkipMode.FastExit)}|{nameof(ArtifactSkipMode.Known)}).", Default = ArtifactSkipMode.None)]
    public ArtifactSkipMode Skip { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public async Task<int> RunAsync()
    {
        if (!ChecksumSource.DefaultSources.ContainsKey(Hash))
        {
            Console.WriteLine($"Failed to find hash algorithm {Hash}\nKnown algorithms:");
            foreach (string id in ChecksumSource.DefaultSources.Values.Select(v => v.Id))
                Console.WriteLine(id);
            return 2;
        }
        ArtifactToolDumpOptions options = new(Update, !Full, Skip, Hash);
        string output = Output ?? Directory.GetCurrentDirectory();
        ArtifactDataManager adm = new DiskArtifactDataManager(output);
        using SqliteArtifactRegistrationManager arm = new(Database);
        IToolLogHandler l = OperatingSystem.IsMacOS() ? ConsoleLogHandler.Fancy : ConsoleLogHandler.Default;
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in ProfileFiles)
            profiles.AddRange(ArtifactToolProfile.DeserializeProfilesFromFile(profileFile));
        profiles = profiles.Select(p => p.GetWithConsoleOptions(CookieFile, Properties)).ToList();
        if (Validate || ValidateOnly)
        {
            Dictionary<ArtifactKey, List<ArtifactResourceInfo>> failed = new();

            void AddFail(ArtifactResourceInfo r)
            {
                if (!failed.TryGetValue(r.Key.Artifact, out var list)) list = failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
                list.Add(r);
            }

            foreach (ArtifactToolProfile profile in profiles)
            {
                try
                {
                    Common.LoadAssemblyForToolString(profile.Tool);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);
                    return 69;
                }
                ArtifactTool tool;
                if (ArtifactToolLoader.TryLoad(profile, out var toolTmp)) tool = toolTmp;
                else
                {
                    Console.WriteLine($"Unknown tool {profile.Tool}");
                    return 88;
                }
                tool.DebugMode = Debug;
                var pp = profile.WithCoreTool(tool);
                l.Log($"Processing entries for profile {pp.Tool}/{pp.Group}", null, LogLevel.Title);
                int i = 0, j = 0;
                foreach (ArtifactInfo inf in await arm.ListArtifactsAsync(pp.Tool, pp.Group))
                {
                    i++;
                    foreach (ArtifactResourceInfo rInf in await arm.ListResourcesAsync(inf.Key))
                    {
                        j++;
                        if (!await adm.ExistsAsync(rInf.Key))
                        {
                            AddFail(rInf);
                            continue;
                        }
                        if (rInf.Checksum == null)
                        {
                            if (AddChecksum) AddFail(rInf);
                            continue;
                        }
                        if (!ChecksumSource.TryGetHashAlgorithm(rInf.Checksum.Id, out HashAlgorithm? hashAlgorithm))
                        {
                            AddFail(rInf);
                            continue;
                        }
                        await using Stream sourceStream = await adm.OpenInputStreamAsync(rInf.Key);
                        await using HashProxyStream hps = new(sourceStream, hashAlgorithm, true);
                        await using MemoryStream ms = new();
                        await hps.CopyToAsync(ms);
                        if (!rInf.Checksum.Value.AsSpan().SequenceEqual(hps.GetHash())) AddFail(rInf);
                    }
                }
                l.Log($"Processed {i} artifacts and {j} resources for profile {pp.Tool}/{pp.Group}", null, LogLevel.Information);
            }
            if (failed.Count == 0)
            {
                l.Log("All resources for specified profiles successfully validated.", null, LogLevel.Information);
                return 0;
            }
            if (ValidateOnly)
            {
                l.Log($"{failed.Sum(v => v.Value.Count)} resources failed to validate.", null, LogLevel.Information);
                return 1;
            }
            l.Log($"{failed.Sum(v => v.Value.Count)} resources failed to validate and will be reacquired.", null, LogLevel.Information);
            foreach (ArtifactToolProfile profile in profiles)
            {
                ArtifactToolProfile artifactToolProfile = profile;
                if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
                try
                {
                    Common.LoadAssemblyForToolString(profile.Tool);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);
                    return 69;
                }
                if (!ArtifactToolLoader.TryLoad(artifactToolProfile, out ArtifactTool? t))
                    throw new ArtifactToolNotFoundException(artifactToolProfile.Tool);
                ArtifactToolConfig config = new(arm, adm, FailureBypassFlags.None);
                using ArtifactTool tool = t;
                tool.DebugMode = Debug;
                artifactToolProfile = artifactToolProfile.WithCoreTool(t);
                if (!failed.Keys.Any(v => v.Tool == artifactToolProfile.Tool && v.Group == artifactToolProfile.Group))
                    continue;
                await tool.InitializeAsync(config, artifactToolProfile).ConfigureAwait(false);
                switch (tool)
                {
                    case IArtifactToolFind:
                        {
                            var proxy = new ArtifactToolFindProxy(tool);
                            foreach ((ArtifactKey key, List<ArtifactResourceInfo> list) in failed.Where(v => v.Key.Tool == artifactToolProfile.Tool && v.Key.Group == artifactToolProfile.Group).ToList())
                                if (await proxy.FindAsync(key.Id) is { } data) await Fixup(tool, l, failed, key, list, data);
                                else l.Log($"Failed to obtain artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                            break;
                        }
                    case IArtifactToolList:
                        {
                            await foreach (ArtifactData data in (new ArtifactToolListProxy(tool, ArtifactToolListOptions.Default, l).ListAsync()))
                                if (failed.TryGetValue(data.Info.Key, out List<ArtifactResourceInfo>? list))
                                    await Fixup(tool, l, failed, data.Info.Key, list, data);
                            break;
                        }
                }
            }
            if (failed.Count != 0)
            {
                l.Log($"Failed to reacquire {failed.Sum(v => v.Value.Count)} resources.", null, LogLevel.Error);
                foreach (ArtifactResourceInfo value in failed.Values.SelectMany(v => v)) Common.Display(value, Detailed);
            }
            else
                l.Log("Successfully reacquired all resources.", null, LogLevel.Information);
        }
        else
            foreach (ArtifactToolProfile profile in profiles)
            {
                try
                {
                    Common.LoadAssemblyForToolString(profile.Tool);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);
                    return 69;
                }
                await ArtifactDumping.DumpAsync(profile, arm, adm, options, l).ConfigureAwait(false);
            }
        return 0;
    }

    private async Task Fixup(ArtifactTool tool, IToolLogHandler l, Dictionary<ArtifactKey, List<ArtifactResourceInfo>> failed, ArtifactKey key, List<ArtifactResourceInfo> list, ArtifactData data)
    {
        foreach (ArtifactResourceInfo resource in list.ToList())
        {
            if (!data.TryGetValue(resource.Key, out ArtifactResourceInfo? resourceActual))
            {
                l.Log($"Failed to obtain resource {resource.GetInfoPathString()} for artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                continue;
            }
            await tool.DumpResourceAsync(resourceActual, ResourceUpdateMode.Hard, l, Hash);
            list.Remove(resource);
        }
        if (list.Count == 0) failed.Remove(key);
    }
}
