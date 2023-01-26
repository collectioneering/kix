using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Text.RegularExpressions;
using Art;
using Art.Common;
using Art.Modular;

namespace kix.Commands;

internal class ToolsCommand : CommandBase
{
    protected Option<string> SearchOption;

    protected Option<bool> DetailedOption;

    public ToolsCommand() : this("tools", "List available tools.")
    {
    }

    public ToolsCommand(string name, string? description = null) : base(name, description)
    {
        SearchOption = new Option<string>(new[] { "-s", "--search" }, "Search pattern.") { ArgumentHelpName = "pattern" };
        AddOption(SearchOption);
        DetailedOption = new Option<bool>(new[] { "--detailed" }, "Show detailed information on entries.");
        AddOption(DetailedOption);
    }

    protected override Task<int> RunAsync(InvocationContext context)
    {
        var manifests = new Dictionary<string, ModuleManifest>();
        ModuleManifest.LoadManifests(manifests);
        foreach (ModuleManifest manifest in manifests.Values)
        {
            Plugin plugin;
            try
            {
                plugin = Plugin.LoadForManifest(manifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load assembly {manifest.Content.Assembly}:\n{ex}");
                continue;
            }
            string? search = context.ParseResult.GetValueForOption(SearchOption);
            Regex? re = search != null ? Common.GetFilterRegex(search, false, false) : null;
            foreach ((Type toolType, string toolString) in plugin.BaseAssembly.GetExportedTypes()
                         .Where(t => t.IsAssignableTo(typeof(IArtifactTool)) && !t.IsAbstract && t.GetConstructor(Array.Empty<Type>()) != null)
                         .Select(v => (ToolType: v, ToolString: ArtifactToolStringUtil.CreateToolString(v)))
                         .Where(v => re?.IsMatch(v.ToolString) ?? true))
                Common.PrintFormat(toolString, context.ParseResult.GetValueForOption(DetailedOption), () =>
                {
                    bool canFind = toolType.IsAssignableTo(typeof(IArtifactToolFind));
                    bool canList = toolType.IsAssignableTo(typeof(IArtifactToolList));
                    bool canDump = canList || toolType.IsAssignableTo(typeof(IArtifactToolDump));
                    IEnumerable<string> capabilities = Enumerable.Empty<string>();
                    if (canFind) capabilities = capabilities.Append("find");
                    if (canList) capabilities = capabilities.Append("list");
                    if (canDump) capabilities = capabilities.Append("arc");
                    capabilities = capabilities.DefaultIfEmpty("none");
                    return new StringBuilder("Capabilities: ").AppendJoin(", ", capabilities).ToString();
                });
        }
        return Task.FromResult(0);
    }
}
