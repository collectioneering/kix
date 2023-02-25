using System.CommandLine;
using System.CommandLine.Invocation;

namespace Art.Tesler;

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
        catch (ArtUserException e)
        {
            PrintExceptionMessage(e);
            return -1;
        }
    }

    protected static void PrintExceptionMessage(Exception e)
    {
        PrintErrorMessage(e.Message);
    }

    protected static void PrintErrorMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    protected static void PrintWarningMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    protected abstract Task<int> RunAsync(InvocationContext context);
}
