namespace Art.Tesler.Profiles;

public interface IResolvedProfiles
{
    IReadOnlyList<ArtifactToolProfile> Values { get; }
}
