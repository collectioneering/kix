using System.CommandLine;
using Art.Common;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler.Config;

public class ConfigCommandGet : ConfigCommandGetSetBase
{
    private readonly IScopedRunnerPropertyProvider _runnerPropertyProvider;
    private readonly IScopedToolPropertyProvider _toolPropertyProvider;
    private readonly IProfileResolver _profileResolver;
    private readonly IArtifactToolRegistryStore _registryStore;
    protected Option<bool> ExactScopeOption;
    protected Option<bool> VerboseOption;
    protected Option<bool> PrettyPrintOption;

    public ConfigCommandGet(
        IOutputControl toolOutput,
        IScopedRunnerPropertyProvider runnerPropertyProvider,
        IScopedToolPropertyProvider toolPropertyProvider,
        IProfileResolver profileResolver,
        IArtifactToolRegistryStore registryStore,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        _runnerPropertyProvider = runnerPropertyProvider;
        _toolPropertyProvider = toolPropertyProvider;
        _profileResolver = profileResolver;
        _registryStore = registryStore;
        ExactScopeOption = new Option<bool>("-e", "--exact-scope") { Description = "Only check at the exact scope" };
        Add(ExactScopeOption);
        VerboseOption = new Option<bool>("-v", "--verbose") { Description = "Use verbose output format" };
        Add(VerboseOption);
        PrettyPrintOption = new Option<bool>("--pretty-print") { Description = "Pretty-print values" };
        Add(PrettyPrintOption);
    }

    protected override Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        bool prettyPrint = parseResult.GetValue(PrettyPrintOption);
        PropertyFormatter propertyFormatter = parseResult.GetValue(VerboseOption)
            ? new DefaultPropertyFormatter(prettyPrint)
            : new PropertyValueFormatter(prettyPrint);
        ConfigScopeFlags configScopeFlags = GetConfigScopeFlags(parseResult);
        string key = parseResult.GetRequiredValue(KeyArgument);
        if (parseResult.GetValue(ToolOption) is { } toolString)
        {
            if (!ArtifactToolIDUtil.TryParseID(toolString, out var toolID))
            {
                PrintErrorMessage($"Unable to parse tool string \"{toolString}\"", ToolOutput);
                return Task.FromResult(1);
            }
            if (TeslerPropertyUtility.TryGetPropertyDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, key, configScopeFlags, out var result))
            {
                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(toolID, result));
            }
        }
        else if (parseResult.GetValue(InputOption) is not null)
        {
            if (!TryGetProfilesWithIndex(_profileResolver, parseResult, out var profiles, out string profileString, out int selectedIndex, out int errorCode))
            {
                return Task.FromResult(errorCode);
            }
            var profile = profiles.Values[selectedIndex];
            if (!ArtifactToolIDUtil.TryParseID(profile.Tool, out var toolID))
            {
                PrintErrorMessage($"Unable to parse tool string \"{profile.Tool}\" for profile {selectedIndex} in {profileString}", ToolOutput);
                return Task.FromResult(1);
            }
            if ((configScopeFlags & ConfigScopeFlags.Profile) != 0 && profile.Options is { } options && options.TryGetValue(key, out var profileValueResult))
            {
                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(selectedIndex, profile, toolID, new ConfigProperty(ConfigScope.Profile, key, profileValueResult)));
            }
            else if (TeslerPropertyUtility.TryGetPropertyDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, key, configScopeFlags, out var result))
            {
                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(selectedIndex, profile, toolID, result));
            }
        }
        else
        {
            if (_runnerPropertyProvider.TryGetProperty(key, configScopeFlags, out var result))
            {
                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(result));
            }
        }
        return Task.FromResult(0);
    }

    private ConfigScopeFlags GetConfigScopeFlags(ParseResult parseResult)
    {
        ConfigScope configScope = GetConfigScope(parseResult);
        bool exactScope = parseResult.GetValue(ExactScopeOption);
        return configScope switch
        {
            ConfigScope.Global => ConfigScopeFlags.Global,
            ConfigScope.Local => exactScope ? ConfigScopeFlags.Local : ConfigScopeFlags.Local | ConfigScopeFlags.Global,
            ConfigScope.Profile => exactScope ? ConfigScopeFlags.Profile : ConfigScopeFlags.Local | ConfigScopeFlags.Global | ConfigScopeFlags.Profile,
            _ => throw new InvalidOperationException($"Invalid scope value {configScope}")
        };
    }
}
