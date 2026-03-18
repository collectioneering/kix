using System.Diagnostics.CodeAnalysis;

namespace Art.Tesler.Profiles;

public class AggregateProfileResolver : IProfileResolver
{
    private readonly IProfileResolver[] _profileResolvers;

    public AggregateProfileResolver(IProfileResolver[] profileResolvers)
    {
        _profileResolvers = profileResolvers;
    }

    public bool TryGetProfiles(string text, [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles, ProfileResolutionFlags profileResolutionFlags = ProfileResolutionFlags.Default)
    {
        foreach (var resolver in _profileResolvers)
        {
            if (resolver.TryGetProfiles(text, out resolvedProfiles, profileResolutionFlags))
            {
                return true;
            }
        }
        resolvedProfiles = null;
        return false;
    }
}
