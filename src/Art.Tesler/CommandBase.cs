using System.CommandLine;
using Art.BrowserCookies;
using Art.EF;

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
        catch (Exception e)
        {
            Type? exceptionType = e.GetType();
            while (exceptionType != null)
            {
                if (exceptionType.FullName is { } fullName && s_filteredExceptions.TryGetValue(fullName, out var knownExceptionDelegate))
                {
                    PrintExceptionMessage(e, ToolOutput);
                    knownExceptionDelegate?.Invoke(e, ToolOutput);
                    return -1;
                }
                exceptionType = exceptionType.BaseType;
            }
            throw;
        }
    }

    private static readonly Dictionary<string, KnownExceptionDelegate?> s_filteredExceptions =
        new()
        {
            { $"Art.BrowserCookies.{nameof(BrowserLookupConfigException)}", null }, //
            { $"Art.EF.{nameof(EFPendingMigrationsPresentException)}", HandleEFPendingMigrationsPresent }, //
        };

    private delegate void KnownExceptionDelegate(Exception e, IOutputControl console);

    private static void HandleEFPendingMigrationsPresent(Exception e, IOutputControl console)
    {
        PrintErrorMessage("Please run migrations on the database prior to using the database in read-only mode.", console);
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
