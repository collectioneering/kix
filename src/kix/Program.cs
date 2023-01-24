using Art.Common;
using CommandLine;

namespace Kix;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var parser = new Parser(v =>
        {
            v.AllowMultiInstance = true;
            v.HelpWriter = Console.Error;
        });
        return await parser
            .ParseArguments<RunArc, RunDbList, RunDbDelete, RunDump, RunFind, RunList, RunRehash, RunTools, RunValidate>(args)
            .MapResult<IRunnable, Task<int>>(async x =>
            {
                try
                {
                    return await x.RunAsync();
                }
                catch (Exception e)
                {
                    if (x.Verbose)
                    {
                        throw;
                    }
                    Console.Error.WriteLine(e.Message);
                    return -1;
                }
            }, _ => Task.FromResult(1)).Caf();
    }
}
