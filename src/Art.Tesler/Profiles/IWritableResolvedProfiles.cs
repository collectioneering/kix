namespace Art.Tesler.Profiles;

public interface IWritableResolvedProfiles : IResolvedProfiles
{
    void WriteProfiles(IReadOnlyList<ArtifactToolProfile> artifactToolProfiles);
}
