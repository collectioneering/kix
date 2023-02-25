using Art.Common;

namespace Art.Modular;

public interface IPluginStore
{
    IArtifactToolRegistry LoadPluginFromToolString(string toolString)
    {
        ArtifactToolID id;
        try
        {
            id = ArtifactToolProfileUtil.GetID(toolString);
        }
        catch (ArgumentException e)
        {
            throw new ArtUserException(e.Message);
        }
        return LoadPluginFromArtifactToolId(id);
    }

    IArtifactToolRegistry LoadPluginFromArtifactToolId(ArtifactToolID artifactToolId);

    IArtifactToolRegistry LoadPluginFromDescription(IPluginDescription pluginDescription);

    IEnumerable<IPluginDescription> GetPluginDescriptions();
}
