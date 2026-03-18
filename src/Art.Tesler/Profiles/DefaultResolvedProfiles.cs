namespace Art.Tesler.Profiles;

public class DefaultResolvedProfiles : IResolvedProfiles
{
    public IReadOnlyList<ArtifactToolProfile> Values { get; }

    public DefaultResolvedProfiles(IReadOnlyList<ArtifactToolProfile> values)
    {
        Values = values;
    }
}
