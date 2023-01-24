using System.CommandLine;

namespace Kix;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new RunArc(),
            new RunDump(),
            new RunFind(),
            new RunList(),
            new RunRehash(),
            new RunTools(),
            new RunValidate(),
            new RunDbList(),
            new RunDbDelete()
        };
        return await rootCommand.InvokeAsync(args);
    }
}
