using System.CommandLine;
using Art.Common;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class ValidateCommand : ToolCommandBase
{
    protected ITeslerDataProvider DataProvider;

    protected ITeslerRegistrationProvider RegistrationProvider;

    protected IExtensionsContext ExtensionsContext;

    protected TimeProvider TimeProvider;

    protected Option<string> HashOption;

    protected Argument<List<string>> ProfileFilesArg;

    protected Option<bool> RepairOption;

    protected Option<bool> AddChecksumOption;

    protected Option<bool> DetailedOption;

    protected Option<bool> ProgressMeterOption;

    public ValidateCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider)
        : this(
            toolLogHandlerProvider,
            pluginStore,
            toolPropertyProvider,
            dataProvider,
            registrationProvider,
            extensionsContext,
            timeProvider,
            "validate",
            "Verify resource integrity.")
    {
    }

    public ValidateCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider,
        string name,
        string? description = null) : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, name, description)
    {
        DataProvider = dataProvider;
        DataProvider.Initialize(this);
        RegistrationProvider = registrationProvider;
        RegistrationProvider.Initialize(this);
        ExtensionsContext = extensionsContext;
        TimeProvider = timeProvider;
        HashOption = new Option<string>("-h", "--hash") { HelpName = Common.ChecksumAlgorithms, Description = "Checksum algorithm", DefaultValueFactory = static _ => Common.DefaultChecksumAlgorithm };
        Add(HashOption);
        ProfileFilesArg = new Argument<List<string>>("profile") { HelpName = "profile", Arity = ArgumentArity.ZeroOrMore, Description = "Profile file(s) to filter and repair with" };
        Add(ProfileFilesArg);
        RepairOption = new Option<bool>("--repair") { Description = "Re-obtain resources that failed validation (requires appropriate profiles)" };
        Add(RepairOption);
        AddChecksumOption = new Option<bool>("--add-checksum") { Description = "Add checksum to resources without checksum during validation" };
        Add(AddChecksumOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
        ProgressMeterOption = new Option<bool>("--progress-meter") { Description = "Show progress meters" };
        Add(ProgressMeterOption);
    }

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        ChecksumSource? checksumSource;
        string? hash = parseResult.GetValue(HashOption);
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        if (hash == null)
        {
            checksumSource = null;
        }
        else
        {
            if (!ChecksumSource.DefaultSources.TryGetValue(hash, out checksumSource))
            {
                PrintErrorMessage(Common.GetInvalidHashMessage(hash), ToolOutput);
                return 2;
            }
        }
        var logPreferences = GetLogPreferences(parseResult);
        IToolLogHandler l = ToolLogHandlerProvider.GetDefaultToolLogHandler(logPreferences);
        List<ArtifactToolProfile> profiles = [];
        foreach (string profileFile in parseResult.GetRequiredValue(ProfileFilesArg))
        {
            profiles.AddRange(ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFile));
        }
        profiles = PrepareProfiles(parseResult, profiles).ToList();
        bool repair = parseResult.GetValue(RepairOption);
        if (profiles.Count == 0)
        {
            if (repair)
            {
                l.Log("Repair was requested, but no profiles were provided", null, LogLevel.Error);
                return 3;
            }
            l.Log("No profiles provided, validating all artifacts and resources", null, LogLevel.Information);
        }
        using var adm = DataProvider.CreateArtifactDataManager(parseResult);
        using var arm = RegistrationProvider.CreateArtifactRegistrationManager(parseResult, isReadonly: !repair);
        bool progressMeter = parseResult.GetValue(ProgressMeterOption);
        var validationContext = new ValidationContext(PluginStore, arm, adm, l, progressMeter);
        ValidationProcessResult result;
        ChecksumSource? checksumSourceForAdd = parseResult.GetValue(AddChecksumOption) ? checksumSource : null;
        if (profiles.Count == 0)
        {
            result = await validationContext.ProcessAsync(await arm.ListArtifactsAsync(cancellationToken).ConfigureAwait(false), checksumSourceForAdd, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            result = await validationContext.ProcessAsync(profiles, checksumSourceForAdd, cancellationToken).ConfigureAwait(false);
        }
        l.Log($"Total: {result.Artifacts} artifacts and {result.Resources} resources processed.", null, LogLevel.Information);
        if (!validationContext.AnyFailed)
        {
            l.Log("All resources for specified profiles successfully validated.", null, LogLevel.Information);
            return 0;
        }
        int resourceFailCount = validationContext.CountResourceFailures();
        if (!repair)
        {
            l.Log($"{resourceFailCount} resources failed to validate.", null, LogLevel.Warning);
            foreach (var entry in validationContext.GetFailureCountsByKey())
            {
                l.Log($"Artifact {entry.Key}: {entry.Value} failures", null, LogLevel.Warning);
            }
            return 1;
        }
        l.Log($"{resourceFailCount} resources failed to validate and will be reacquired.", null, LogLevel.Information);
        var repairContext = validationContext.CreateRepairContext();
        (bool getArtifactRetrievalTimestamps, bool getResourceRetrievalTimestamps, bool debugMode) = GetArtifactRetrievalOptions(parseResult);
        await repairContext.RepairAsync(
            profiles,
            parseResult.GetValue(DetailedOption),
            checksumSource,
            ExtensionsContext,
            TimeProvider,
            getArtifactRetrievalTimestamps,
            getResourceRetrievalTimestamps,
            debugMode,
            ToolOutput,
            cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
