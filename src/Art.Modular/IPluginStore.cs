namespace Art.Modular;

public interface IPluginStore
{
    IPlugin LoadPluginFromToolString(string toolString);

    IPlugin LoadPluginFromDescription(IPluginDescription pluginDescription);

    IEnumerable<IPluginDescription> GetPluginDescriptions();
}
