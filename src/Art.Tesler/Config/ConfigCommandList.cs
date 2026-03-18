using System.CommandLine;
using Art.Common;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler.Config;

public class ConfigCommandList : CommandBase
{
    private readonly IScopedRunnerPropertyProvider _runnerPropertyProvider;
    private readonly IScopedToolPropertyProvider _toolPropertyProvider;
    private readonly IProfileResolver _profileResolver;
    private readonly IArtifactToolRegistryStore _registryStore;

    protected Option<bool> LocalOption;
    protected Option<bool> GlobalOption;
    protected Option<bool> ProfileOption;
    protected Option<bool> AllOption;
    protected Option<string> InputOption;
    protected Option<string> ToolOption;
    protected Option<bool> EffectiveOption;
    protected Option<bool> IgnoreBaseTypesOption;
    protected Option<bool> VerboseOption;
    protected Option<bool> PrettyPrintOption;

    public ConfigCommandList(
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
        ToolOption = new Option<string>("-t", "--tool") { HelpName = "tool-string", Description = "Tool for which to get configuration properties" };
        Add(ToolOption);
        InputOption = new Option<string>("-i", "--input") { HelpName = "profile-path", Description = "Profile for which to get configuration properties" };
        Add(InputOption);
        LocalOption = new Option<bool>("-l", "--local") { Description = "Get properties in local option scope" };
        Add(LocalOption);
        GlobalOption = new Option<bool>("-g", "--global") { Description = "Get properties in global option scope" };
        Add(GlobalOption);
        ProfileOption = new Option<bool>("-p", "--profile") { Description = "Get properties in profile option scope" };
        Add(ProfileOption);
        AllOption = new Option<bool>("-a", "--all") { Description = "Get properties in all option scopes" };
        Add(AllOption);
        EffectiveOption = new Option<bool>("-e", "--effective") { Description = "Gets effective config values (default)" };
        Add(EffectiveOption);
        IgnoreBaseTypesOption = new Option<bool>("--ignore-base-types") { Description = "(Tools and profiles) Ignores base types" };
        Add(IgnoreBaseTypesOption);
        VerboseOption = new Option<bool>("-v", "--verbose") { Description = "Use verbose output format" };
        Add(VerboseOption);
        PrettyPrintOption = new Option<bool>("--pretty-print") { Description = "Pretty-print values" };
        Add(PrettyPrintOption);
        Validators.Add(result =>
        {
            var optionSet = new HashSet<Option>();
            if (result.GetValue(ToolOption) != null)
            {
                optionSet.Add(ToolOption);
            }

            if (result.GetValue(InputOption) != null)
            {
                optionSet.Add(InputOption);
            }

            if (optionSet.Count > 1)
            {
                result.AddError($"Only one option from {CommandHelper.GetOptionAliasList([ToolOption, InputOption])} may be specified");
                return;
            }

            bool anyScopeSpecifiers = result.GetValue(AllOption) || result.GetValue(LocalOption) || result.GetValue(GlobalOption) || result.GetValue(ProfileOption);

            if (result.GetValue(EffectiveOption))
            {
                if (anyScopeSpecifiers || result.GetValue(IgnoreBaseTypesOption))
                {
                    result.AddError($"{CommandHelper.GetOptionAlias(EffectiveOption)} may not be used with options {CommandHelper.GetOptionAliasList([AllOption, LocalOption, GlobalOption, IgnoreBaseTypesOption])}");
                    return;
                }
            }

            if (result.GetValue(ProfileOption))
            {
                if (result.GetValue(InputOption) == null)
                {
                    result.AddError($"{CommandHelper.GetOptionAlias(ProfileOption)} must be used with {CommandHelper.GetOptionAlias(InputOption)}");
                    return;
                }
            }

            if (result.GetValue(IgnoreBaseTypesOption))
            {
                if (result.GetValue(ToolOption) == null && result.GetValue(InputOption) == null)
                {
                    result.AddError($"{CommandHelper.GetOptionAlias(IgnoreBaseTypesOption)} must be used with {CommandHelper.GetOptionAlias(ToolOption)} or {CommandHelper.GetOptionAlias(InputOption)}");
                }
            }
        });
    }

    protected override Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        bool prettyPrint = parseResult.GetValue(PrettyPrintOption);
        PropertyFormatter propertyFormatter = parseResult.GetValue(VerboseOption)
            ? new DefaultPropertyFormatter(prettyPrint)
            : new SimplePropertyFormatter(prettyPrint);
        ListingSettings listingSettings = GetListingSettings(parseResult);
        if (parseResult.GetValue(ToolOption) is { } toolString)
        {
            if (!ArtifactToolIDUtil.TryParseID(toolString, out var toolID))
            {
                PrintErrorMessage($"Unable to parse tool string \"{toolString}\"", ToolOutput);
                return Task.FromResult(1);
            }
            switch (listingSettings)
            {
                case ScopedListingSettings scopedListingSettings:
                    {
                        if (scopedListingSettings.IgnoreBaseTypes)
                        {
                            foreach (var v in _toolPropertyProvider.GetProperties(toolID, scopedListingSettings.ConfigScopeFlags))
                            {
                                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(toolID, v));
                            }
                        }
                        else
                        {
                            foreach (var v in TeslerPropertyUtility.GetPropertiesDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, scopedListingSettings.ConfigScopeFlags))
                            {
                                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(toolID, v));
                            }
                        }
                        break;
                    }
                case EffectiveListingSettings:
                    {
                        var map = new Dictionary<string, ConfigProperty>();
                        foreach (var v in TeslerPropertyUtility.GetPropertiesDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, ConfigScopeFlags.All))
                        {
                            map[v.Key] = v;
                        }
                        foreach (var v in map.Values)
                        {
                            ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(toolID, v));
                        }
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid listing setting type {listingSettings.GetType()}");
            }
            return Task.FromResult(0);
        }
        if (parseResult.GetValue(InputOption) is { } profileString)
        {
            if (!_profileResolver.TryGetProfiles(profileString, out var profiles, ProfileResolutionFlags.Files))
            {
                PrintErrorMessage($"Unable to identify profile file {profileString}", ToolOutput);
                return Task.FromResult(2);
            }

            var profileList = profiles.Values;
            for (int i = 0; i < profileList.Count; i++)
            {
                var profile = profileList[i];
                if (!ArtifactToolIDUtil.TryParseID(profile.Tool, out var toolID))
                {
                    PrintErrorMessage($"Unable to parse tool string \"{profile.Tool}\" in profile index {i}", ToolOutput);
                    return Task.FromResult(1);
                }
                switch (listingSettings)
                {
                    case ScopedListingSettings scopedListingSettings:
                        {
                            if (scopedListingSettings.IgnoreBaseTypes)
                            {
                                foreach (var v in _toolPropertyProvider.GetProperties(toolID, scopedListingSettings.ConfigScopeFlags))
                                {
                                    ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(i, profile, toolID, v));
                                }
                            }
                            else
                            {
                                foreach (var v in TeslerPropertyUtility.GetPropertiesDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, scopedListingSettings.ConfigScopeFlags))
                                {
                                    ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(i, profile, toolID, v));
                                }
                            }
                            if ((scopedListingSettings.ConfigScopeFlags & ConfigScopeFlags.Profile) != 0 && profile.Options != null)
                            {
                                foreach (var v in profile.Options)
                                {
                                    ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(i, profile, toolID, new ConfigProperty(ConfigScope.Profile, v.Key, v.Value)));
                                }
                            }
                            break;
                        }
                    case EffectiveListingSettings:
                        {
                            var map = new Dictionary<string, ConfigProperty>();
                            foreach (var v in TeslerPropertyUtility.GetPropertiesDeep(_registryStore, _toolPropertyProvider, ToolOutput, toolID, ConfigScopeFlags.All))
                            {
                                map[v.Key] = v;
                            }
                            if (profile.Options != null)
                            {
                                foreach (var v in profile.Options)
                                {
                                    map[v.Key] = new ConfigProperty(ConfigScope.Profile, v.Key, v.Value);
                                }
                            }
                            foreach (var v in map.Values)
                            {
                                ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(i, profile, toolID, v));
                            }
                            break;
                        }
                    default:
                        throw new InvalidOperationException($"Invalid listing setting type {listingSettings.GetType()}");
                }
            }
            return Task.FromResult(0);
        }
        switch (listingSettings)
        {
            case ScopedListingSettings scopedListingSettings:
                {
                    foreach (var v in _runnerPropertyProvider.GetProperties(scopedListingSettings.ConfigScopeFlags))
                    {
                        ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(v));
                    }
                    break;
                }
            case EffectiveListingSettings:
                {
                    Dictionary<string, ConfigProperty> map = new();
                    foreach (var v in _runnerPropertyProvider.GetProperties(ConfigScopeFlags.All))
                    {
                        map[v.Key] = v;
                    }
                    foreach (var v in map.Values)
                    {
                        ToolOutput.Out.WriteLine(propertyFormatter.FormatProperty(v));
                    }
                    break;
                }
            default:
                throw new InvalidOperationException($"Invalid listing setting type {listingSettings.GetType()}");
        }
        return Task.FromResult(0);
    }

    private record ListingSettings;

    private record ScopedListingSettings(ConfigScopeFlags ConfigScopeFlags, bool IgnoreBaseTypes) : ListingSettings;

    private record EffectiveListingSettings : ListingSettings;

    private ListingSettings GetListingSettings(ParseResult parseResult)
    {
        if (parseResult.GetValue(EffectiveOption))
        {
            return new EffectiveListingSettings();
        }
        ConfigScopeFlags? activeFlags = null;
        if (parseResult.GetValue(AllOption))
        {
            activeFlags = (activeFlags ?? ConfigScopeFlags.None) | ConfigScopeFlags.All;
        }
        if (parseResult.GetValue(LocalOption))
        {
            activeFlags = (activeFlags ?? ConfigScopeFlags.None) | ConfigScopeFlags.Local;
        }
        if (parseResult.GetValue(GlobalOption))
        {
            activeFlags = (activeFlags ?? ConfigScopeFlags.None) | ConfigScopeFlags.Global;
        }
        if (parseResult.GetValue(ProfileOption))
        {
            activeFlags = (activeFlags ?? ConfigScopeFlags.None) | ConfigScopeFlags.Profile;
        }
        if (activeFlags is { } flags)
        {
            return new ScopedListingSettings(flags, parseResult.GetValue(IgnoreBaseTypesOption));
        }
        return new EffectiveListingSettings();
    }
}
