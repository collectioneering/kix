using System.CommandLine;
using Art.Modular;
using Art.Tesler.Cookie;
using Art.Tesler.Database;

namespace Art.Tesler;

public class TeslerRootCommand<TPluginStore> : RootCommand where TPluginStore : IPluginStore
{
    protected TPluginStore PluginStore;

    public TeslerRootCommand(TPluginStore pluginStore)
    {
        PluginStore = pluginStore;
    }

    public static TeslerRootCommand<TPluginStore> Create(TPluginStore pluginStore)
    {
        return new TeslerRootCommand<TPluginStore>(pluginStore)
        {
            new ArcCommand<TPluginStore>(pluginStore),
            new DumpCommand<TPluginStore>(pluginStore),
            new FindCommand<TPluginStore>(pluginStore),
            new ListCommand<TPluginStore>(pluginStore),
            new RehashCommand(),
            new ToolsCommand<TPluginStore>(pluginStore),
            new ValidateCommand<TPluginStore>(pluginStore),
            new DatabaseCommand(),
            new CookieCommand()
        };
    }
}
