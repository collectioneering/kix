using System.Diagnostics.CodeAnalysis;
using Art.Common;

namespace Art.Tesler.Profiles;

public class DiskProfileResolver : IProfileResolver
{
    public bool TryGetProfiles(string text, [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles, ProfileResolutionFlags profileResolutionFlags = ProfileResolutionFlags.Default)
    {
        if ((profileResolutionFlags & ProfileResolutionFlags.Files) != 0 && File.Exists(text))
        {
            var profiles = ArtifactToolProfileUtil.DeserializeProfilesFromFile(text, out bool isSingleObject);
            resolvedProfiles = new FileResolvedProfiles(profiles, Path.GetFullPath(text), isSingleObject);
            return true;
        }
        resolvedProfiles = null;
        return false;
    }
}
