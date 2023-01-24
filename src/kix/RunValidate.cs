using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art;
using Art.Common;
using Art.Common.Management;
using Art.EF.Sqlite;

namespace Kix;

internal class RunValidate : ToolCommand
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<string> HashOption;

    protected Argument<List<string>> ProfileFilesArg;

    protected Option<bool> RepairOption;

    protected Option<bool> AddChecksumOption;

    protected Option<bool> DetailedOption;

    public RunValidate() : this("validate", "Verify resource integrity.")
    {
    }

    public RunValidate(string name, string? description = null) : base(name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file.");
        DatabaseOption.ArgumentHelpName = "file";
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory.");
        OutputOption.ArgumentHelpName = "directory";
        OutputOption.SetDefaultValue(Directory.GetCurrentDirectory());
        AddOption(OutputOption);
        HashOption = new Option<string>(new[] { "-h", "--hash" }, "Checksum algorithm (None|SHA1|SHA256|SHA384|SHA512|MD5).");
        HashOption.SetDefaultValue("SHA256");
        AddOption(HashOption);
        ProfileFilesArg = new Argument<List<string>>("profile", "Profile file(s) to filter and repair with.");
        ProfileFilesArg.HelpName = "file";
        ProfileFilesArg.Arity = ArgumentArity.ZeroOrMore;
        AddArgument(ProfileFilesArg);
        RepairOption = new Option<bool>(new[] { "--repair" }, "Re-obtain resources that failed validation (requires appropriate profiles)");
        AddOption(RepairOption);
        AddChecksumOption = new Option<bool>(new[] { "--add-checksum" }, "Add checksum to resources without checksum during validation.");
        AddOption(AddChecksumOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries.");
        AddOption(DetailedOption);
        this.SetHandler(RunAsync);
    }

    public async Task<int> RunAsync(InvocationContext context)
    {
        string? hash = context.ParseResult.HasOption(HashOption) ? context.ParseResult.GetValueForOption(HashOption) : null;
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        if (hash != null && !ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            Console.WriteLine($"Failed to find hash algorithm {hash}\nKnown algorithms:");
            foreach (string id in ChecksumSource.DefaultSources.Values.Select(v => v.Id))
                Console.WriteLine(id);
            return 2;
        }
        IToolLogHandler l = Common.GetDefaultToolLogHandler();
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in context.ParseResult.GetValueForArgument(ProfileFilesArg))
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile, JsonOpt.Options));
        string? cookieFile = context.ParseResult.HasOption(CookieFileOption) ? context.ParseResult.GetValueForOption(CookieFileOption) : null;
        IEnumerable<string> properties = context.ParseResult.HasOption(PropertiesOption) ? context.ParseResult.GetValueForOption(PropertiesOption)! : Array.Empty<string>();
        profiles = profiles.Select(p => p.GetWithConsoleOptions(cookieFile, properties)).ToList();
        bool repair = context.ParseResult.GetValueForOption(RepairOption);
        if (profiles.Count == 0)
        {
            if (repair)
            {
                l.Log("Repair was requested, but no profiles were provided", null, LogLevel.Error);
                return 3;
            }
            l.Log("No profiles provided, validating all artifacts and resources", null, LogLevel.Information);
        }
        ArtifactDataManager adm = new DiskArtifactDataManager(context.ParseResult.GetValueForOption(OutputOption)!);
        using SqliteArtifactRegistrationManager arm = new(context.ParseResult.GetValueForOption(DatabaseOption)!);
        var validationContext = new ValidationContext(arm, adm, context.ParseResult.GetValueForOption(AddChecksumOption), l);
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
        if (!repair)
        {
            l.Log($"{resourceFailCount} resources failed to validate.", null, LogLevel.Information);
            return 1;
        }
        l.Log($"{resourceFailCount} resources failed to validate and will be reacquired.", null, LogLevel.Information);
        var repairContext = validationContext.CreateRepairContext();
        await repairContext.RepairAsync(profiles, context.ParseResult.GetValueForOption(DetailedOption), hash);
        return 0;
    }
}
