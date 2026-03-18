using System.Diagnostics.CodeAnalysis;

namespace Art.Tesler.Profiles;

public interface IProfileResolver
{
    bool TryGetProfiles(string text, [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles, ProfileResolutionFlags profileResolutionFlags = ProfileResolutionFlags.Default);
}
