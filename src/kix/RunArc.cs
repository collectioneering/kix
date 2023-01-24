using Art;
using Art.Common;
using Art.Common.Management;
using Art.Common.Proxies;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("arc", HelpText = "Execute archival artifact tools.")]
internal class RunArc : BRunTool, IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = Common.DefaultDbFile)]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option('h', "hash", HelpText = "Checksum algorithm (e.g. None|SHA1|SHA256|SHA384|SHA512|MD5).", Default = "SHA256")]
    public string Hash { get; set; } = null!;

    [Value(0, HelpText = "Profile file.", MetaValue = "file", MetaName = "profileFile", Required = true)]
    public IReadOnlyCollection<string> ProfileFiles { get; set; } = null!;

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
        ArtifactToolDumpOptions options = new(Update, !Full, FastExit ? ArtifactSkipMode.FastExit : Skip, hash);
        IArtifactDataManager adm = NullOutput ? new NullArtifactDataManager() : new DiskArtifactDataManager(Output ?? Directory.GetCurrentDirectory());
        using SqliteArtifactRegistrationManager arm = new(Database);
        IToolLogHandler l = Common.GetDefaultToolLogHandler();
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in ProfileFiles)
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options));
        profiles = profiles.Select(p => p.GetWithConsoleOptions(CookieFile, Properties)).ToList();
        foreach (ArtifactToolProfile profile in profiles)
        {
            Plugin context;
            try
            {
                context = Plugin.LoadForToolString(profile.Tool);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
                return 69;
            }
            await ArtifactDumping.DumpAsync(context.Context, profile, arm, adm, options, l).ConfigureAwait(false);
        }
        return 0;
    }
}
