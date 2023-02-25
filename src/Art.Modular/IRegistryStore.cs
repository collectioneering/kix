using Art.Common;

namespace Art.Modular;

public interface IRegistryStore
{
    IArtifactToolRegistry LoadRegistry(ArtifactToolID artifactToolId);

    IEnumerable<IArtifactToolRegistry> LoadAllRegistries();
}
