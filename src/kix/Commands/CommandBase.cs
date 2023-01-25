using System.CommandLine;
using System.CommandLine.Invocation;
using Art;

namespace kix.Commands;

public abstract class CommandBase : Command
{
    protected CommandBase(string name, string? description = null) : base(name, description)
    {
        this.SetHandler(RunInternalAsync);
    }

    private async Task<int> RunInternalAsync(InvocationContext context)
    {
        try
        {
            return await RunAsync(context);
        }
        catch (ManifestNotFoundException e)
        {
            PrintExceptionMessage(e);
            return -1;
        }
        catch (ArtifactToolNotFoundException e)
        {
            PrintExceptionMessage(e);
            return -1;
        }
    }

    private static void PrintExceptionMessage(Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(e.Message);
        Console.ResetColor();
    }

    protected abstract Task<int> RunAsync(InvocationContext context);
}
