namespace Art.Modular;

public interface IPluginStore
{
    IPlugin LoadForToolString(string toolString);
}
