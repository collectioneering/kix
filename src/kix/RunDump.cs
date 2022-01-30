using Art;
using Art.EF.Sqlite;
using CommandLine;
using kix;

namespace Kix;

[Verb("dump", HelpText = "Execute artifact dump tools.")]
internal class RunDump : BRunTool, IRunnable
{
    [Option('d', "database", HelpText = "Sqlite database file.", MetaValue = "file", Default = "kix_data.db")]
    public string Database { get; set; } = null!;

    [Option('o', "output", HelpText = "Output directory.", MetaValue = "directory")]
    public string? Output { get; set; }

    [Option("no-database", HelpText = "Don't use database to track resources.")] public bool NoDatabase { get; set; }

    [Option('i', "input", HelpText = "Profile file.", MetaValue = "file", Group = "source")]
    public string? ProfileFile { get; set; } = null!;

    [Option('t', "tool", HelpText = "Tool to use or filter profiles by.", MetaValue = "name", Group = "source")]
    public string? Tool { get; set; }

    [Option('g', "group", HelpText = "Group to use or filter profiles by.", MetaValue = "name")]
    public string? Group { get; set; } = null!;

    public async Task<int> RunAsync()
    {
        string output = Output ?? Directory.GetCurrentDirectory();
        ArtifactDataManager adm = new DiskArtifactDataManager(output);
        if (NoDatabase)
        {
            InMemoryArtifactRegistrationManager arm = new();
            return await RunAsync(adm, arm);
        }
        else
        {
            using SqliteArtifactRegistrationManager arm = new(Database);
            return await RunAsync(adm, arm);
        }
    }

    private async Task<int> RunAsync(ArtifactDataManager adm, ArtifactRegistrationManager arm)
    {
        if (ProfileFile == null) return await ExecAsync(new ArtifactToolProfile(Tool!, Group ?? "default", null), arm, adm);
        int ec = 0;
        foreach (ArtifactToolProfile profile in ArtifactToolProfile.DeserializeProfilesFromFile(ProfileFile))
        {
            if (Group != null && Group != profile.Group || Tool != null && Tool != profile.Tool) continue;
            ec = Math.Max(await ExecAsync(profile, arm, adm), ec);
        }
        return ec;
    }

    private async Task<int> ExecAsync(ArtifactToolProfile profile, ArtifactRegistrationManager arm, ArtifactDataManager adm)
    {
        ArtifactToolDumpOptions options = new();
        ArtifactTool t;
        try
        {
            t = await GetToolAsync(profile, arm, adm);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return 69;
        }
        using var tool = t;
        ArtifactToolDumpProxy dProxy = new(tool, options, OperatingSystem.IsMacOS() ? ConsoleLogHandler.Fancy : ConsoleLogHandler.Default);
        await dProxy.DumpAsync();
        return 0;
    }
}
