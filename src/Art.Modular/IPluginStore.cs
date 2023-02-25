using Art.Common;

namespace Art.Modular;

public interface IPluginStore
{
    IPlugin LoadPluginFromToolString(string toolString)
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

    IPlugin LoadPluginFromArtifactToolId(ArtifactToolID artifactToolId);

    IPlugin LoadPluginFromDescription(IPluginDescription pluginDescription);

    IEnumerable<IPluginDescription> GetPluginDescriptions();
}
