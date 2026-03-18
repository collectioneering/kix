using System.CommandLine;
using Art.Common.Proxies;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class FindCommand : FindListCommandBase
{
    protected Argument<List<string>> IdsArg;

    public FindCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider
    )
        : this(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, "find", "Execute artifact finder tools.")
    {
    }

    public FindCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider,
        string name,
        string? description = null) :
        base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, name, description)
    {
        IdsArg = new Argument<List<string>>("ids") { HelpName = "id", Arity = ArgumentArity.OneOrMore, Description = "IDs" };
        Add(IdsArg);
    }

    protected override async Task ExecuteAsync(
        ParseResult parseResult,
        IToolLogHandler logHandler,
        bool listResource,
        bool detailed,
        IArtifactTool tool,
        CancellationToken cancellationToken)
    {
        ArtifactToolFindProxy proxy = new(tool, logHandler);
        foreach (string id in parseResult.GetRequiredValue(IdsArg))
        {
            IArtifactData? data = null;
            try
            {
                data = await proxy.FindAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PrintExceptionMessage(ex, ToolOutput);
                continue;
            }
            finally
            {
                if (data == null) PrintWarningMessage($"!! [{id}] not found", ToolOutput);
            }
            if (data != null)
            {
                await Common.DisplayAsync(
                        data,
                        listResource,
                        detailed,
                        ToolOutput,
                        logHandler.LogPreferences)
                    .ConfigureAwait(false);
            }
        }
    }
}
