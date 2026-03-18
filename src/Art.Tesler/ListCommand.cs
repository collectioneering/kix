using System.CommandLine;
using Art.Common.Proxies;
using Art.Tesler.Properties;

namespace Art.Tesler;

public class ListCommand : FindListCommandBase
{
    public ListCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider
    )
        : this(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, "list", "Execute artifact list tools.")
    {
    }

    public ListCommand(
        IToolLogHandlerProvider toolLogHandlerProvider,
        IArtifactToolRegistryStore pluginStore,
        IToolPropertyProvider toolPropertyProvider,
        TimeProvider timeProvider,
        string name,
        string? description = null)
        : base(toolLogHandlerProvider, pluginStore, toolPropertyProvider, timeProvider, name, description)
    {
    }

    protected override async Task ExecuteAsync(
        ParseResult parseResult,
        IToolLogHandler logHandler,
        bool listResource,
        bool detailed,
        IArtifactTool tool,
        CancellationToken cancellationToken)
    {
        ArtifactToolListOptions options = new();
        ArtifactToolListProxy proxy = new(tool, options, logHandler);
        await foreach (IArtifactData data in proxy.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            await Common.DisplayAsync(
                data,
                listResource,
                detailed,
                ToolOutput,
                logHandler.LogPreferences).ConfigureAwait(false);
        }
    }
}
