using System.CommandLine;

namespace Art.Tesler;

public abstract class CommandBase : Command
{
    protected IOutputControl ToolOutput;

    protected CommandBase(IOutputControl toolOutput, string name, string? description = null) : base(name, description)
    {
        ToolOutput = toolOutput;
        SetAction(RunInternalAsync);
    }

    private async Task<int> RunInternalAsync(ParseResult parseResult)
    {
        try
        {
            return await RunAsync(parseResult, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ArtUserException e)
        {
            PrintExceptionMessage(e, ToolOutput);
            return -1;
        }
    }

    protected static void PrintExceptionMessage(Exception e, IOutputControl console)
    {
        PrintErrorMessage(e.Message, console);
    }

    protected static void PrintErrorMessage(string message, IOutputControl console)
    {
        console.Error.WriteLine(message);
    }

    protected static void PrintWarningMessage(string message, IOutputControl console)
    {
        console.Error.WriteLine(message);
    }

    protected abstract Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
