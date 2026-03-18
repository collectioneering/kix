using System.Diagnostics.CodeAnalysis;

namespace Art.Tesler.Profiles;

public class DictionaryProfileResolver : IProfileResolver
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ArtifactToolProfile>> _profileMap;

    public DictionaryProfileResolver(IReadOnlyDictionary<string, IReadOnlyList<ArtifactToolProfile>> profileMap)
    {
        _profileMap = profileMap;
    }

    public bool TryGetProfiles(string text, [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles, ProfileResolutionFlags profileResolutionFlags = ProfileResolutionFlags.Default)
    {
        if ((profileResolutionFlags & ProfileResolutionFlags.Files) != 0 && _profileMap.TryGetValue(text, out var profileArr))
        {
            resolvedProfiles = new DefaultResolvedProfiles(profileArr);
            return true;
        }
        resolvedProfiles = null;
        return false;
    }
}
