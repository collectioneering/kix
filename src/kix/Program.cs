using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using kix.Commands;
using kix.Commands.Cookie;
using kix.Commands.Database;

namespace kix;

internal static class Program
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
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
