using Art;
using Art.Common;
using Art.Common.Management;
using Art.EF.Sqlite;
using CommandLine;

namespace Kix;

[Verb("validate", HelpText = "Verify resource integrity.")]
internal class RunValidate : BRunTool, IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = Common.DefaultDbFile)]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option('h', "hash", HelpText = "Checksum algorithm (e.g. None|SHA1|SHA256|SHA384|SHA512|MD5).", Default = "SHA256")]
    public string Hash { get; set; } = null!;

    [Value(0, HelpText = "Profile file(s) to filter and repair with.", MetaValue = "file", MetaName = "profileFile")]
    public IReadOnlyCollection<string> ProfileFiles { get; set; } = null!;

    [Option("repair", HelpText = "Re-obtain resources that failed validation (requires appropriate profiles).")]
    public bool Repair { get; set; }

    [Option("add-checksum", HelpText = "Add checksum to resources without checksum during validation.")]
    public bool AddChecksum { get; set; }

    [Option("detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

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
        IToolLogHandler l = Common.GetDefaultToolLogHandler();
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in ProfileFiles)
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options));
        profiles = profiles.Select(p => p.GetWithConsoleOptions(CookieFile, Properties)).ToList();
        if (profiles.Count == 0)
        {
            if (Repair)
            {
                l.Log("Repair was requested, but no profiles were provided", null, LogLevel.Error);
                return 3;
            }
            l.Log("No profiles provided, validating all artifacts and resources", null, LogLevel.Information);
        }
        ArtifactDataManager adm = new DiskArtifactDataManager(Output ?? Directory.GetCurrentDirectory());
        using SqliteArtifactRegistrationManager arm = new(Database);
        var validationContext = new ValidationContext(arm, adm, AddChecksum, l);
        ValidationProcessResult result;
        if (profiles.Count == 0) result = await validationContext.ProcessAsync(await arm.ListArtifactsAsync());
        else result = await validationContext.ProcessAsync(profiles);
        l.Log($"Total: {result.Artifacts} artifacts and {result.Resources} processed.", null, LogLevel.Information);
        if (!validationContext.AnyFailed)
        {
            l.Log("All resources for specified profiles successfully validated.", null, LogLevel.Information);
            return 0;
        }
        int resourceFailCount = validationContext.CountResourceFailures();
        if (!Repair)
        {
            l.Log($"{resourceFailCount} resources failed to validate.", null, LogLevel.Information);
            return 1;
        }
        l.Log($"{resourceFailCount} resources failed to validate and will be reacquired.", null, LogLevel.Information);
        var repairContext = validationContext.CreateRepairContext();
        await repairContext.RepairAsync(profiles, Detailed, Hash);
        return 0;
    }
}
