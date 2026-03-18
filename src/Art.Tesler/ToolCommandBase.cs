using System.CommandLine;
using Art.Common;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler;

public abstract class ToolCommandBase : CommandBase
{
    protected IToolLogHandlerProvider ToolLogHandlerProvider;

    protected IArtifactToolRegistryStore PluginStore;

    protected IToolPropertyProvider ToolPropertyProvider;

    protected Option<string> UserAgentOption;

    protected Option<string> CookieFileOption;

    protected Option<List<string>> PropertiesOption;

    protected Option<List<string>> PropertyElementsOption;

    protected Option<bool> NoDefaultPropertiesOption;

    protected Option<bool> NoRetrievalTimestampsOption;

    protected Option<bool> NoArtifactRetrievalTimestampsOption;

    protected Option<bool> NoResourceRetrievalTimestampsOption;

    protected Option<DataUnits> LogDataUnitsOption;

    protected Option<DataUnitFormat> LogDataUnitFormatOption;

    protected ToolCommandBase(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        string name,
        string? description = null) : base(toolLogHandlerProvider, name, description)
    {
        ToolLogHandlerProvider = toolLogHandlerProvider;
        PluginStore = pluginStore;
        ToolPropertyProvider = toolPropertyProvider;
        UserAgentOption = new Option<string>("--user-agent") { HelpName = "user-agent", Description = "Custom user agent string" };
        Add(UserAgentOption);
        CookieFileOption = new Option<string>("--cookie-file") { HelpName = "file", Description = "Cookie file" };
        Add(CookieFileOption);
        PropertiesOption = new Option<List<string>>("-p", "--property") { HelpName = "key:value", Arity = ArgumentArity.ZeroOrMore, Description = "Add a property" };
        Add(PropertiesOption);
        PropertyElementsOption = new Option<List<string>>("--property-element") { HelpName = "key:value", Arity = ArgumentArity.ZeroOrMore, Description = "Add an element to a list property" };
        Add(PropertyElementsOption);
        NoDefaultPropertiesOption = new Option<bool>("--no-default-properties") { Description = "Don't apply default properties" };
        Add(NoDefaultPropertiesOption);
        NoRetrievalTimestampsOption = new Option<bool>("--no-retrieval-timestamps") { Description = "Don't apply retrieval timestamps" };
        Add(NoRetrievalTimestampsOption);
        NoArtifactRetrievalTimestampsOption = new Option<bool>("--no-artifact-retrieval-timestamps") { Description = "Don't apply artifact retrieval timestamps" };
        Add(NoArtifactRetrievalTimestampsOption);
        NoResourceRetrievalTimestampsOption = new Option<bool>("--no-resource-retrieval-timestamps") { Description = "Don't apply resource retrieval timestamps" };
        Add(NoResourceRetrievalTimestampsOption);
        LogDataUnitsOption = new Option<DataUnits>("--log-data-units") { Description = $"Data size units ({Common.DataUnitsModes})" };
        LogDataUnitsOption.HelpName = "units";
        LogDataUnitsOption.DefaultValueFactory = static _ => DataUnits.Binary;
        Add(LogDataUnitsOption);
        LogDataUnitFormatOption = new Option<DataUnitFormat>("--log-data-unit-format") { Description = $"Data size format ({Common.DataUnitFormatModes})" };
        LogDataUnitFormatOption.HelpName = "format";
        LogDataUnitFormatOption.DefaultValueFactory = static _ => DataUnitFormat.Short;
        Add(LogDataUnitFormatOption);
    }

    protected ArtifactToolProfile PrepareProfile(ParseResult parseResult, ArtifactToolProfile artifactToolProfile)
    {
        PopulateOutputs(parseResult, out string? cookieFile, out string? userAgent, out IReadOnlyCollection<string> properties, out IReadOnlyCollection<string> propertyElements);
        var toolPropertyProvider = GetOptionalToolPropertyProvider(parseResult);
        return artifactToolProfile.GetWithConsoleOptions(PluginStore, toolPropertyProvider, properties, propertyElements, cookieFile, userAgent, ToolOutput);
    }

    protected IEnumerable<ArtifactToolProfile> PrepareProfiles(ParseResult parseResult, IEnumerable<ArtifactToolProfile> artifactToolProfiles)
    {
        PopulateOutputs(parseResult, out string? cookieFile, out string? userAgent, out IReadOnlyCollection<string> properties, out IReadOnlyCollection<string> propertyElements);
        var toolPropertyProvider = GetOptionalToolPropertyProvider(parseResult);
        return artifactToolProfiles.Select(p => p.GetWithConsoleOptions(PluginStore, toolPropertyProvider, properties, propertyElements, cookieFile, userAgent, ToolOutput));
    }

    private void PopulateOutputs(
        ParseResult parseResult,
        out string? cookieFile,
        out string? userAgent,
        out IReadOnlyCollection<string> properties,
        out IReadOnlyCollection<string> propertyElements)
    {
        cookieFile = parseResult.GetValue(CookieFileOption);
        userAgent = parseResult.GetValue(UserAgentOption);
        properties = (IReadOnlyCollection<string>?)parseResult.GetValue(PropertiesOption) ?? [];
        propertyElements = (IReadOnlyCollection<string>?)parseResult.GetValue(PropertyElementsOption) ?? [];
    }

    protected (bool getArtifactRetrievalTimestamps, bool getResourceRetrievalTimestamps) GetArtifactRetrievalOptions(ParseResult parseResult)
    {
        bool noRetrievalTimestamps = parseResult.GetValue(NoRetrievalTimestampsOption);
        bool noArtifactRetrievalTimestamps = noRetrievalTimestamps || parseResult.GetValue(NoArtifactRetrievalTimestampsOption);
        bool noResourceRetrievalTimestamps = noRetrievalTimestamps || parseResult.GetValue(NoResourceRetrievalTimestampsOption);
        return (getArtifactRetrievalTimestamps: !noArtifactRetrievalTimestamps, getResourceRetrievalTimestamps: !noResourceRetrievalTimestamps);
    }

    protected LogPreferences GetLogPreferences(ParseResult parseResult)
    {
        DataUnits dataUnits = parseResult.GetRequiredValue(LogDataUnitsOption);
        DataUnitFormat dataUnitFormat = parseResult.GetRequiredValue(LogDataUnitFormatOption);
        return new LogPreferences(DataUnits: dataUnits, DataUnitFormat: dataUnitFormat);
    }

    protected async Task<IArtifactTool> GetToolAsync(
        ArtifactToolProfile artifactToolProfile,
        IArtifactRegistrationManager arm,
        IArtifactDataManager adm,
        TimeProvider timeProvider,
        bool getArtifactRetrievalTimestamps,
        bool getResourceRetrievalTimestamps,
        CancellationToken cancellationToken = default)
    {
        if (!PluginStore.TryLoadRegistry(ArtifactToolIDUtil.ParseID(artifactToolProfile.Tool), out var plugin))
        {
            throw new ArtifactToolNotFoundException(artifactToolProfile.Tool);
        }
        return await ArtifactTool.PrepareToolAsync(
                plugin,
                artifactToolProfile,
                arm,
                adm,
                timeProvider,
                getArtifactRetrievalTimestamps,
                getResourceRetrievalTimestamps,
                cancellationToken)
            .ConfigureAwait(false);
    }

    protected IToolPropertyProvider? GetOptionalToolPropertyProvider(ParseResult parseResult)
    {
        if (parseResult.GetValue(NoDefaultPropertiesOption))
        {
            return null;
        }
        return ToolPropertyProvider;
    }

    protected static void ResolveAndAddProfiles(IProfileResolver profileResolver, List<ArtifactToolProfile> profiles, string profileFile)
    {
        if (!profileResolver.TryGetProfiles(profileFile, out var profilesResult))
        {
            throw new ArtUserException($"Could not resolve profile for input \"{profileFile}\"");
        }
        profiles.AddRange(profilesResult.Values);
    }
}
