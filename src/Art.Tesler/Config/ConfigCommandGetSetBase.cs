using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler.Config;

public abstract class ConfigCommandGetSetBase : CommandBase
{
    protected const int ErrorProfileLoad = 2;
    protected const int ErrorProfileIndexUnspecified = 3;
    protected const int ErrorProfileIndexInvalid = 4;
    protected Option<bool> LocalOption;
    protected Option<bool> GlobalOption;
    protected Option<bool> ProfileOption;
    protected Option<string> InputOption;
    protected Option<string> ToolOption;
    protected Argument<string> KeyArgument;
    protected Option<int?> ProfileIndexOption;

    protected ConfigCommandGetSetBase(
        IOutputControl toolOutput,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        ToolOption = new Option<string>("-t", "--tool") { HelpName = "tool-string", Description = "Tool for which to get/set configuration property" };
        Add(ToolOption);
        InputOption = new Option<string>("-i", "--input") { HelpName = "profile-path", Description = "Profile for which to get/set configuration property" };
        Add(InputOption);
        LocalOption = new Option<bool>("-l", "--local") { Description = "Use local option scope" };
        Add(LocalOption);
        GlobalOption = new Option<bool>("-g", "--global") { Description = "Use global option scope" };
        Add(GlobalOption);
        ProfileOption = new Option<bool>("-p", "--profile") { Description = "Use profile option scope" };
        Add(ProfileOption);
        KeyArgument = new Argument<string>("key") { HelpName = "key", Arity = ArgumentArity.ExactlyOne, Description = "Configuration property key" };
        Add(KeyArgument);
        ProfileIndexOption = new Option<int?>("--profile-index") { Description = "Profile index" };
        Add(ProfileIndexOption);
        Validators.Add(result =>
        {
            HashSet<Option> optionSet = [];
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

            optionSet.Clear();

            if (result.GetValue(LocalOption))
            {
                optionSet.Add(LocalOption);
            }
            if (result.GetValue(GlobalOption))
            {
                optionSet.Add(GlobalOption);
            }
            if (result.GetValue(ProfileOption))
            {
                optionSet.Add(ProfileOption);
            }

            if (optionSet.Count > 1)
            {
                result.AddError($"Only one option from {CommandHelper.GetOptionAliasList([LocalOption, GlobalOption, ProfileOption])} may be specified");
                return;
            }

            if (result.GetValue(ProfileOption) && result.GetValue(InputOption) == null)
            {
                result.AddError($"{CommandHelper.GetOptionAlias(ProfileOption)} may not be used without {CommandHelper.GetOptionAlias(InputOption)}");
                return;
            }

            if (result.GetValue(ProfileIndexOption) != null && result.GetValue(InputOption) == null)
            {
                result.AddError($"{CommandHelper.GetOptionAlias(ProfileIndexOption)} may not be used without {CommandHelper.GetOptionAlias(InputOption)}");
                return;
            }

            if (result.GetValue(InputOption) != null)
            {
                if (result.GetValue(LocalOption) && result.GetValue(GlobalOption))
                {
                    result.AddError($"{CommandHelper.GetOptionAliasList([LocalOption, GlobalOption])} may not be specified when {CommandHelper.GetOptionAlias(InputOption)} is specified");
                }
            }
        });
    }

    protected bool TryGetProfilesWithIndex(
        IProfileResolver profileResolver,
        ParseResult parseResult,
        [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles,
        out string profileString,
        out int selectedIndex,
        out int errorCode)
    {
        profileString = parseResult.GetRequiredValue(InputOption);
        if (!profileResolver.TryGetProfiles(profileString, out resolvedProfiles, ProfileResolutionFlags.Files))
        {
            PrintErrorMessage($"Unable to identify profile file {profileString}", ToolOutput);
            resolvedProfiles = null;
            selectedIndex = -1;
            errorCode = ErrorProfileLoad;
            return false;
        }

        var profileList = resolvedProfiles.Values;
        if (parseResult.GetValue(ProfileIndexOption) is { } profileIndexResult)
        {
            selectedIndex = profileIndexResult;
        }
        else if (profileList.Count != 1)
        {
            PrintErrorMessage($"There are {profileList.Count} profiles in profile file {profileString} - select one with {CommandHelper.GetOptionAlias(ProfileIndexOption)}", ToolOutput);
            selectedIndex = -1;
            errorCode = ErrorProfileIndexUnspecified;
            return false;
        }
        else
        {
            selectedIndex = 0;
        }
        if ((uint)selectedIndex >= profileList.Count)
        {
            PrintErrorMessage($"{CommandHelper.GetOptionAlias(ProfileIndexOption)} {selectedIndex} is out of range for {profileList.Count} profiles in profile file {profileString}", ToolOutput);
            errorCode = ErrorProfileIndexInvalid;
            return false;
        }
        errorCode = 0;
        return true;
    }

    protected ConfigScope GetConfigScope(ParseResult parseResult)
    {
        if (parseResult.GetValue(ProfileOption))
        {
            return ConfigScope.Profile;
        }
        if (parseResult.GetValue(GlobalOption))
        {
            return ConfigScope.Global;
        }
        if (parseResult.GetValue(LocalOption))
        {
            return ConfigScope.Local;
        }
        return parseResult.GetValue(InputOption) is not null ? ConfigScope.Profile : ConfigScope.Local;
    }
}
