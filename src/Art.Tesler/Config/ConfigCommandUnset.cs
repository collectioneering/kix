using System.CommandLine;
using Art.Common;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler.Config;

public class ConfigCommandUnset : ConfigCommandGetSetBase
{
    private readonly IWritableScopedRunnerPropertyProvider _runnerPropertyProvider;
    private readonly IWritableScopedToolPropertyProvider _toolPropertyProvider;
    private readonly IProfileResolver _profileResolver;

    public ConfigCommandUnset(
        IOutputControl toolOutput,
        IWritableScopedRunnerPropertyProvider runnerPropertyProvider,
        IWritableScopedToolPropertyProvider toolPropertyProvider,
        IProfileResolver profileResolver,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        _runnerPropertyProvider = runnerPropertyProvider;
        _toolPropertyProvider = toolPropertyProvider;
        _profileResolver = profileResolver;
    }

    protected override Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        ConfigScope configScope = GetConfigScope(parseResult);
        string key = parseResult.GetRequiredValue(KeyArgument);
        ConfigPropertyIdentifier propertyIdentifier = new(configScope, key);
        if (parseResult.GetValue(ToolOption) is { } toolString)
        {
            if (!ArtifactToolIDUtil.TryParseID(toolString, out var toolID))
            {
                PrintErrorMessage($"Unable to parse tool string \"{toolString}\"", ToolOutput);
                return Task.FromResult(1);
            }
            switch (configScope)
            {
                case ConfigScope.Local:
                    if (!TryUnsetToolPropertyNonProfile(toolID, new ConfigPropertyIdentifier(ConfigScope.Local, key)))
                    {
                        return Task.FromResult(1);
                    }
                    break;
                case ConfigScope.Global:
                    if (!TryUnsetToolPropertyNonProfile(toolID, new ConfigPropertyIdentifier(ConfigScope.Global, key)))
                    {
                        return Task.FromResult(1);
                    }
                    break;
                case ConfigScope.Profile:
                default:
                    throw new InvalidOperationException($"Invalid config scope {configScope} for tool");
            }
        }
        else if (parseResult.GetValue(InputOption) is not null)
        {
            if (!TryGetProfilesWithIndex(_profileResolver, parseResult, out var profiles, out string profileString, out int selectedIndex, out int errorCode))
            {
                return Task.FromResult(errorCode);
            }
            if (profiles is not IWritableResolvedProfiles writableResolvedProfiles)
            {
                PrintErrorMessage($"Source for profiles in {profileString} is not writable", ToolOutput);
                return Task.FromResult(7);
            }
            var profile = profiles.Values[selectedIndex];
            if (!ArtifactToolIDUtil.TryParseID(profile.Tool, out var toolID))
            {
                PrintErrorMessage($"Unable to parse tool string \"{profile.Tool}\" for profile {selectedIndex} in {profileString}", ToolOutput);
                return Task.FromResult(1);
            }
            switch (configScope)
            {
                case ConfigScope.Local:
                    if (!TryUnsetToolPropertyNonProfile(toolID, new ConfigPropertyIdentifier(ConfigScope.Local, key)))
                    {
                        return Task.FromResult(1);
                    }
                    break;
                case ConfigScope.Global:
                    if (!TryUnsetToolPropertyNonProfile(toolID, new ConfigPropertyIdentifier(ConfigScope.Global, key)))
                    {
                        return Task.FromResult(1);
                    }
                    break;
                case ConfigScope.Profile:
                    List<ArtifactToolProfile> copy = new(profiles.Values) { [selectedIndex] = profile with { Options = TeslerPropertyUtility.GetOptionsMapWithRemovedKey(profile.Options, key) } };
                    writableResolvedProfiles.WriteProfiles(copy);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid config scope {configScope} for profile {selectedIndex} in {profileString}");
            }
        }
        else
        {
            if (!TryUnsetRunnerProperty(propertyIdentifier))
            {
                return Task.FromResult(1);
            }
        }
        return Task.FromResult(0);
    }

    private bool TryUnsetToolPropertyNonProfile(ArtifactToolID artifactToolId, ConfigPropertyIdentifier configPropertyIdentifier)
    {
        if (!_toolPropertyProvider.TryUnsetProperty(artifactToolId, configPropertyIdentifier))
        {
            PrintFailureToRemove(configPropertyIdentifier);
            return false;
        }
        return true;
    }

    private bool TryUnsetRunnerProperty(ConfigPropertyIdentifier configPropertyIdentifier)
    {
        if (!_runnerPropertyProvider.TryUnsetProperty(configPropertyIdentifier))
        {
            PrintFailureToRemove(configPropertyIdentifier);
            return false;
        }
        return true;
    }

    private void PrintFailureToRemove(ConfigPropertyIdentifier configPropertyIdentifier)
    {
        PrintErrorMessage($"Failed to set property {ConfigPropertyUtility.FormatPropertyKeyForDisplay(configPropertyIdentifier)}", ToolOutput);
    }
}
