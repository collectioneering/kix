using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Text.RegularExpressions;
using Art.Common;

namespace Art.Tesler;

internal class ToolsCommand<TPluginStore> : CommandBase where TPluginStore : IRegistryStore
{
    protected TPluginStore PluginStore;

    protected Option<string> SearchOption;

    protected Option<bool> DetailedOption;

    public ToolsCommand(TPluginStore pluginStore) : this(pluginStore, "tools", "List available tools.")
    {
    }

    public ToolsCommand(TPluginStore pluginStore, string name, string? description = null) : base(name, description)
    {
        PluginStore = pluginStore;
        SearchOption = new Option<string>(new[] { "-s", "--search" }, "Search pattern") { ArgumentHelpName = "pattern" };
        AddOption(SearchOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries");
        AddOption(DetailedOption);
    }

    protected override Task<int> RunAsync(InvocationContext context)
    {
        foreach (var plugin in PluginStore.LoadAllRegistries())
        {
            string? search = context.ParseResult.GetValueForOption(SearchOption);
            Regex? re = search != null ? Common.GetFilterRegex(search, false, false) : null;
            foreach (var desc in plugin.GetToolDescriptions()
                         .Where(v => re?.IsMatch(v.Id.GetToolString()) ?? true))
            {
                Common.PrintFormat(desc.Id.GetToolString(), context.ParseResult.GetValueForOption(DetailedOption), () =>
                {
                    bool canFind = desc.Type.IsAssignableTo(typeof(IArtifactToolFind));
                    bool canList = desc.Type.IsAssignableTo(typeof(IArtifactToolList));
                    bool canDump = canList || desc.Type.IsAssignableTo(typeof(IArtifactToolDump));
                    IEnumerable<string> capabilities = Enumerable.Empty<string>();
                    if (canFind) capabilities = capabilities.Append("find");
                    if (canList) capabilities = capabilities.Append("list");
                    if (canDump) capabilities = capabilities.Append("arc");
                    capabilities = capabilities.DefaultIfEmpty("none");
                    return new StringBuilder("Capabilities: ").AppendJoin(", ", capabilities).ToString();
                });
            }
        }
        return Task.FromResult(0);
    }
}
