using Art;
using Art.Crypto;
using Art.EF.Sqlite;
using Art.Logging;
using Art.Management;
using Art.Proxies;
using CommandLine;

namespace Kix;

[Verb("arc", HelpText = "Execute archival artifact tools.")]
internal class RunArc : BRunTool, IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = "kix_data.db")]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option('h', "hash", HelpText = "Checksum algorithm (e.g. None|SHA1|SHA256|SHA384|SHA512|MD5).", Default = "SHA256")]
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

    [Option('z', "fast-exit", HelpText = $"Equivalent to -s/--skip {nameof(ArtifactSkipMode.FastExit)}.")]
    public bool FastExit { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    [Option("null-output", HelpText = "Send resources to the void.")]
    public bool NullOutput { get; set; }

    public async Task<int> RunAsync()
    {
        string? hash = string.Equals(Hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : Hash;
        if (hash != null && !ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            Console.WriteLine($"Failed to find hash algorithm {hash}\nKnown algorithms:");
            foreach (string id in ChecksumSource.DefaultSources.Values.Select(v => v.Id))
                Console.WriteLine(id);
            return 2;
        }
        if ((Validate || ValidateOnly) && NullOutput)
        {
            Console.WriteLine("Null output mode cannot be used in conjunction with validation mode.");
            return 1234;
        }
        ArtifactToolDumpOptions options = new(Update, !Full, FastExit ? ArtifactSkipMode.FastExit : Skip, hash);
        ArtifactDataManager adm = NullOutput ? new NullArtifactDataManager() : new DiskArtifactDataManager(Output ?? Directory.GetCurrentDirectory());
        using SqliteArtifactRegistrationManager arm = new(Database);
        IToolLogHandler l = OperatingSystem.IsMacOS() ? ConsoleLogHandler.Fancy : ConsoleLogHandler.Default;
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in ProfileFiles)
            profiles.AddRange(ArtifactToolProfile.DeserializeProfilesFromFile(profileFile, JsonOpt.Options));
        profiles = profiles.Select(p => p.GetWithConsoleOptions(CookieFile, Properties)).ToList();
        if (Validate || ValidateOnly)
        {
            var validationContext = new ValidationContext(arm, adm, Debug, AddChecksum, l);
            await validationContext.ProcessAsync(profiles);
            if (!validationContext.AnyFailed)
            {
                l.Log("All resources for specified profiles successfully validated.", null, LogLevel.Information);
                return 0;
            }
            int resourceFailCount = validationContext.CountResourceFailures();
            if (ValidateOnly)
            {
                l.Log($"{resourceFailCount} resources failed to validate.", null, LogLevel.Information);
                return 1;
            }
            l.Log($"{resourceFailCount} resources failed to validate and will be reacquired.", null, LogLevel.Information);
            var repairContext = validationContext.CreateRepairContext();
            await repairContext.RepairAsync(profiles, Detailed, Hash);
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
}
