using Art.Common;
using Art.Modular;

namespace Art.Tesler;

internal class RegistryPluginStore : IPluginStore
{
    private readonly IArtifactToolRegistry _artifactToolRegistry;

    public RegistryPluginStore(IArtifactToolRegistry artifactToolRegistry) => _artifactToolRegistry = artifactToolRegistry;

    public IArtifactToolRegistry LoadPluginFromArtifactToolId(ArtifactToolID artifactToolId)
    {
        if (_artifactToolRegistry.Contains(artifactToolId))
        {
            throw new ArtUserException($"Registry does not contain an artifact with the ID {artifactToolId}");
        }
        return _artifactToolRegistry;
    }

    public IArtifactToolRegistry LoadPluginFromDescription(IPluginDescription pluginDescription)
    {
        if (pluginDescription is not RegistryPluginDescription desc)
        {
            throw new ArgumentException("Cannot produce plugin for incompatible description type", nameof(pluginDescription));
        }
        return desc.Store._artifactToolRegistry;
    }

    public IEnumerable<IPluginDescription> GetPluginDescriptions()
    {
        yield return new RegistryPluginDescription(this);
    }

    private record RegistryPluginDescription(RegistryPluginStore Store) : IPluginDescription
    {
        public string Name => nameof(RegistryPluginStore);
    }
}
