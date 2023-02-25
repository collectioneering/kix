using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Art.Modular;
using kix.Commands;
using kix.Commands.Cookie;
using kix.Commands.Database;

namespace kix;

internal static class Program
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static async Task<int> Main(string[] args)
    {
        var pluginStore = new PluginStore();
        var rootCommand = new RootCommand
        {
            new ArcCommand<PluginStore>(pluginStore),
            new DumpCommand<PluginStore>(pluginStore),
            new FindCommand<PluginStore>(pluginStore),
            new ListCommand<PluginStore>(pluginStore),
            new RehashCommand(),
            new ToolsCommand<PluginStore>(pluginStore),
            new ValidateCommand<PluginStore>(pluginStore),
            new DatabaseCommand(),
            new CookieCommand()
        };
        return await rootCommand.InvokeAsync(args);
    }
}
