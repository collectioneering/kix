using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using Art;

namespace kix.Commands;

public abstract class CommandBase : Command
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    protected CommandBase(string name, string? description = null) : base(name, description)
    {
        this.SetHandler(RunInternalAsync);
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
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

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    protected abstract Task<int> RunAsync(InvocationContext context);
}
