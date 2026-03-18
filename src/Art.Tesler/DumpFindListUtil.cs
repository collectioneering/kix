using System.CommandLine;
using Art.Common;

namespace Art.Tesler;

internal static class DumpFindListUtil
{
    public static List<ArtifactToolProfile> GetProfiles(IToolGroupOrProfileFileOptions optionsSource, ParseResult parseResult)
    {
        List<ArtifactToolProfile> profiles = [];
        string? profileFileValue = parseResult.GetValue(optionsSource.ProfileFileOption);
        string? toolValue = parseResult.GetValue(optionsSource.ToolOption);
        string? groupValue = parseResult.GetValue(optionsSource.GroupOption);
        if (profileFileValue == null)
        {
            profiles.Add(new ArtifactToolProfile(toolValue!, groupValue, null));
        }
        else
        {
            foreach (ArtifactToolProfile profile in ArtifactToolProfileUtil.DeserializeProfilesFromFile(profileFileValue))
            {
                if (groupValue != null && groupValue != profile.Group || toolValue != null && toolValue != profile.Tool) continue;
                profiles.Add(profile);
            }
        }
        return profiles;
    }
}
