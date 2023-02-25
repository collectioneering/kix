using Art.Common;
using Art.Modular;

namespace Art.Tesler;

internal class StaticRegistryStore : IRegistryStore
{
    private readonly IArtifactToolRegistry _artifactToolRegistry;

    public StaticRegistryStore(IArtifactToolRegistry artifactToolRegistry) => _artifactToolRegistry = artifactToolRegistry;

    public IArtifactToolRegistry LoadRegistry(ArtifactToolID artifactToolId)
    {
        if (_artifactToolRegistry.Contains(artifactToolId))
        {
            throw new ArtUserException($"Registry does not contain an artifact with the ID {artifactToolId}");
        }
        return _artifactToolRegistry;
    }

    public IEnumerable<IArtifactToolRegistry> LoadAllRegistries()
    {
        yield return _artifactToolRegistry;
    }
}
