using System.CommandLine;
using Art.Common;
using Art.Tesler.Config;
using Art.Tesler.Cookie;
using Art.Tesler.Database;
using Art.Tesler.Profiles;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class TeslerRootCommand : RootCommand
{
    protected IArtifactToolRegistryStore PluginStore;

    public TeslerRootCommand(IArtifactToolRegistryStore pluginStore)
    {
        PluginStore = pluginStore;
    }

    public static TeslerRootCommand Create(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IWritableScopedRunnerPropertyProvider runnerPropertyProvider,
        IWritableScopedToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
    {
        return new TeslerRootCommand(pluginStore)
        {
            new ArcCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, profileResolver),
            new DumpCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new FindCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new ListCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new StreamCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, profileResolver),
            new RehashCommand(toolLogHandlerProvider, dataProvider, registrationProvider),
            new ToolsCommand(toolLogHandlerProvider, pluginStore),
            new ValidateCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new DatabaseCommand(toolLogHandlerProvider, registrationProvider, inputRegistrationProvider),
            new CookieCommand(toolLogHandlerProvider),
            new ConfigCommand(toolLogHandlerProvider, runnerPropertyProvider, toolPropertyProvider, profileResolver, pluginStore)
        };
    }

    public static TeslerRootCommand CreateSinglePlugin<TTool>(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IWritableScopedRunnerPropertyProvider runnerPropertyProvider,
        IWritableScopedToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
        where TTool : IArtifactToolFactory
    {
        var registry = new ArtifactToolRegistry();
        registry.Add<TTool>();
        var pluginStore = new StaticArtifactToolRegistryStore(registry);
        return new TeslerRootCommand(pluginStore)
        {
            new ArcCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, profileResolver),
            new DumpCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new FindCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new ListCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new StreamCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, profileResolver),
            new RehashCommand(toolLogHandlerProvider, dataProvider, registrationProvider),
            new ToolsCommand(toolLogHandlerProvider, pluginStore),
            new ValidateCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new DatabaseCommand(toolLogHandlerProvider, registrationProvider, inputRegistrationProvider),
            new CookieCommand(toolLogHandlerProvider),
            new ConfigCommand(toolLogHandlerProvider, runnerPropertyProvider, toolPropertyProvider, profileResolver, pluginStore)
        };
    }

    public static TeslerRootCommand CreateSingleSelectablePlugin<TTool>(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IWritableScopedRunnerPropertyProvider runnerPropertyProvider,
        IWritableScopedToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
        where TTool : IArtifactToolFactory, IArtifactToolSelector<string>
    {
        var registry = new ArtifactToolRegistry();
        registry.AddSelectable<TTool>();
        var pluginStore = new StaticArtifactToolRegistryStore(registry);
        return new TeslerRootCommand(pluginStore)
        {
            new ArcCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider, profileResolver),
            new DumpCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new FindCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new ListCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider),
            new StreamCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, profileResolver),
            new RehashCommand(toolLogHandlerProvider, dataProvider, registrationProvider),
            new ToolsCommand(toolLogHandlerProvider, pluginStore),
            new ValidateCommand(toolLogHandlerProvider, pluginStore, toolPropertyProvider, dataProvider, registrationProvider, timeProvider),
            new DatabaseCommand(toolLogHandlerProvider, registrationProvider, inputRegistrationProvider),
            new CookieCommand(toolLogHandlerProvider),
            new ConfigCommand(toolLogHandlerProvider, runnerPropertyProvider, toolPropertyProvider, profileResolver, pluginStore)
        };
    }

    public static TeslerRootCommand Create(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistry artifactToolRegistry,
        IWritableScopedRunnerPropertyProvider runnerPropertyProvider,
        IWritableScopedToolPropertyProvider toolPropertyProvider,
        ITeslerDataProvider dataProvider,
        ITeslerRegistrationProvider registrationProvider,
        ITeslerRegistrationProvider inputRegistrationProvider,
        TimeProvider timeProvider,
        IProfileResolver profileResolver)
    {
        return Create(
            toolLogHandlerProvider,
            new StaticArtifactToolRegistryStore(artifactToolRegistry),
            runnerPropertyProvider,
            toolPropertyProvider,
            dataProvider,
            registrationProvider,
            inputRegistrationProvider,
            timeProvider,
            profileResolver);
    }
}
