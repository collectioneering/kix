using System.CommandLine;
using kix.Commands;
using kix.Commands.Cookie;
using kix.Commands.Database;

namespace kix;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new ArcCommand(),
            new DumpCommand(),
            new FindCommand(),
            new ListCommand(),
            new RehashCommand(),
            new ToolsCommand(),
            new ValidateCommand(),
            new DatabaseCommand(),
            new CookieCommand()
        };
        return await rootCommand.InvokeAsync(args);
    }
}
