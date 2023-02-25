using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Art.Common;
using Art.Common.Management;
using Art.EF.Sqlite;

namespace Art.Tesler;

internal class ValidateCommand<TPluginStore> : ToolCommandBase<TPluginStore> where TPluginStore : IRegistryStore
{
    protected Option<string> DatabaseOption;

    protected Option<string> OutputOption;

    protected Option<string> HashOption;

    protected Argument<List<string>> ProfileFilesArg;

    protected Option<bool> RepairOption;

    protected Option<bool> AddChecksumOption;

    protected Option<bool> DetailedOption;

    public ValidateCommand(TPluginStore pluginStore) : this(pluginStore, "validate", "Verify resource integrity.")
    {
    }

    public ValidateCommand(TPluginStore pluginStore, string name, string? description = null) : base(pluginStore, name, description)
    {
        DatabaseOption = new Option<string>(new[] { "-d", "--database" }, "Sqlite database file") { ArgumentHelpName = "file" };
        DatabaseOption.SetDefaultValue(Common.DefaultDbFile);
        AddOption(DatabaseOption);
        OutputOption = new Option<string>(new[] { "-o", "--output" }, "Output directory") { ArgumentHelpName = "directory" };
        OutputOption.SetDefaultValue(Directory.GetCurrentDirectory());
        AddOption(OutputOption);
        HashOption = new Option<string>(new[] { "-h", "--hash" }, $"Checksum algorithm ({Common.ChecksumAlgorithms})");
        HashOption.SetDefaultValue(Common.DefaultChecksumAlgorithm);
        AddOption(HashOption);
        ProfileFilesArg = new Argument<List<string>>("profile", "Profile file(s) to filter and repair with") { HelpName = "profile", Arity = ArgumentArity.ZeroOrMore };
        AddArgument(ProfileFilesArg);
        RepairOption = new Option<bool>(new[] { "--repair" }, "Re-obtain resources that failed validation (requires appropriate profiles)");
        AddOption(RepairOption);
        AddChecksumOption = new Option<bool>(new[] { "--add-checksum" }, "Add checksum to resources without checksum during validation");
        AddOption(AddChecksumOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries");
        AddOption(DetailedOption);
    }

    protected override async Task<int> RunAsync(InvocationContext context)
    {
        string? hash = context.ParseResult.HasOption(HashOption) ? context.ParseResult.GetValueForOption(HashOption) : null;
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        if (hash != null && !ChecksumSource.DefaultSources.ContainsKey(hash))
        {
            PrintErrorMessage(Common.GetInvalidHashMessage(hash));
            return 2;
        }
        IToolLogHandler l = Common.GetDefaultToolLogHandler();
        List<ArtifactToolProfile> profiles = new();
        foreach (string profileFile in context.ParseResult.GetValueForArgument(ProfileFilesArg))
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile));
        string? cookieFile = context.ParseResult.HasOption(CookieFileOption) ? context.ParseResult.GetValueForOption(CookieFileOption) : null;
        string? userAgent = context.ParseResult.HasOption(UserAgentOption) ? context.ParseResult.GetValueForOption(UserAgentOption) : null;
        IEnumerable<string> properties = context.ParseResult.HasOption(PropertiesOption) ? context.ParseResult.GetValueForOption(PropertiesOption)! : Array.Empty<string>();
        profiles = profiles.Select(p => p.GetWithConsoleOptions(properties, cookieFile, userAgent)).ToList();
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
        var validationContext = new ValidationContext<TPluginStore>(PluginStore, arm, adm, l);
        ValidationProcessResult result;
        string? hashForAdd = context.ParseResult.GetValueForOption(AddChecksumOption) ? hash : null;
        if (profiles.Count == 0) result = await validationContext.ProcessAsync(await arm.ListArtifactsAsync(), hashForAdd);
        else result = await validationContext.ProcessAsync(profiles, hashForAdd);
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
