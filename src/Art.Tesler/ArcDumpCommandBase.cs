using System.CommandLine;
using Art.Common;
using Art.Common.Management;
using Art.Common.Proxies;
using Art.Tesler.Properties;

namespace Art.Tesler;

public abstract class ArcDumpCommandBase : ToolCommandBase
{
    protected ITeslerDataProvider DataProvider;

    protected ITeslerRegistrationProvider RegistrationProvider;

    protected IExtensionsContext ExtensionsContext;

    protected TimeProvider TimeProvider;

    protected Option<string> HashOption;

    protected Option<bool> NoDatabaseOption;

    protected Option<bool> NullOutputOption;

    public ArcDumpCommandBase(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider,
        string name,
        string? description = null)
        : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, name, description)
    {
        DataProvider = dataProvider;
        DataProvider.Initialize(this);
        RegistrationProvider = registrationProvider;
        RegistrationProvider.Initialize(this);
        ExtensionsContext = extensionsContext;
        TimeProvider = timeProvider;
        HashOption = new Option<string>("-h", "--hash") { HelpName = Common.ChecksumAlgorithms, Description = "Checksum algorithm", DefaultValueFactory = static _ => Common.DefaultChecksumAlgorithm };
        Add(HashOption);
        NoDatabaseOption = new Option<bool>("--no-database") { Description = "Don't use database to track resources" };
        Add(NoDatabaseOption);
        NullOutputOption = new Option<bool>("--null-output") { Description = "Send resources to the void" };
        Add(NullOutputOption);
    }

    protected abstract IReadOnlyList<ArtifactToolProfile> GetProfiles(ParseResult parseResult);

    protected abstract ArtifactToolDumpOptions GetArtifactToolDumpOptions(ParseResult parseResult, ChecksumSource? checksumSource);

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? hash = parseResult.GetValue(HashOption);
        hash = string.Equals(hash, "none", StringComparison.InvariantCultureIgnoreCase) ? null : hash;
        ChecksumSource? checksumSource;
        if (hash == null)
        {
            checksumSource = null;
        }
        else if (!ChecksumSource.DefaultSources.TryGetValue(hash, out checksumSource))
        {
            PrintErrorMessage(Common.GetInvalidHashMessage(hash), ToolOutput);
            return 2;
        }
        var profiles = GetProfiles(parseResult);
        var options = GetArtifactToolDumpOptions(parseResult, checksumSource);
        using var adm = parseResult.GetValue(NullOutputOption)
            ? new NullArtifactDataManager()
            : DataProvider.CreateArtifactDataManager(parseResult);
        using var arm = parseResult.GetValue(NoDatabaseOption)
            ? new NullArtifactRegistrationManager()
            : RegistrationProvider.CreateArtifactRegistrationManager(parseResult, isReadonly: false);
        var logPreferences = GetLogPreferences(parseResult);
        IToolLogHandler l = ToolLogHandlerProvider.GetDefaultToolLogHandler(logPreferences);
        (bool getArtifactRetrievalTimestamps, bool getResourceRetrievalTimestamps, bool debugMode) = GetArtifactRetrievalOptions(parseResult);
        foreach (ArtifactToolProfile profile in PrepareProfiles(parseResult, profiles))
        {
            // no-db mode should use in-memory db to keep tool happy, but specific to each tool run
            using var armNoDb = parseResult.GetValue(NoDatabaseOption) ? new InMemoryArtifactRegistrationManager() : null;
            using var tool = await GetToolAsync(profile, armNoDb ?? arm, adm, ExtensionsContext, TimeProvider, getArtifactRetrievalTimestamps, getResourceRetrievalTimestamps, debugMode, cancellationToken).ConfigureAwait(false);
            await new ArtifactToolDumpProxy(tool, options, l).DumpAsync(cancellationToken).ConfigureAwait(false);
        }
        return 0;
    }
}
