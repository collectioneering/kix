using Art.Common;

namespace Art.Modular;

public interface IRegistryStore
{
    IArtifactToolRegistry LoadRegistryFromToolString(string toolString)
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
        return LoadRegistryFromArtifactToolId(id);
    }

    IArtifactToolRegistry LoadRegistryFromArtifactToolId(ArtifactToolID artifactToolId);

    IEnumerable<IArtifactToolRegistry> LoadAllRegistries();
}
