using Art.Common;

namespace Art.Tesler;

public class ToolControlContext
{
    protected readonly IArtifactToolRegistryStore PluginStore;

    public ToolControlContext(IArtifactToolRegistryStore pluginStore)
    {
        PluginStore = pluginStore;
    }

    protected IArtifactTool LoadTool(ArtifactToolProfile artifactToolProfile)
    {
        if (PluginStore.TryLoadRegistry(ArtifactToolIDUtil.ParseID(artifactToolProfile.Tool), out var context)
            && context.TryLoad(artifactToolProfile.GetID(), out IArtifactTool? t))
        {
            return t;
        }
        throw new ArtifactToolNotFoundException(artifactToolProfile.Tool);
    }
}
