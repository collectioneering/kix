using System.CommandLine;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Art.Common;

namespace Art.Tesler;

public class ToolsCommand : CommandBase
{
    protected IArtifactToolRegistryStore PluginStore;

    protected Option<string> SearchOption;

    protected Option<bool> DetailedOption;

    public ToolsCommand(
        IOutputControl toolOutput,
        IArtifactToolRegistryStore pluginStore)
        : this(toolOutput, pluginStore, "tools", "List available tools.")
    {
    }

    public ToolsCommand(
        IOutputControl toolOutput,
        IArtifactToolRegistryStore pluginStore,
        string name,
        string? description = null)
        : base(toolOutput, name, description)
    {
        PluginStore = pluginStore;
        SearchOption = new Option<string>("-s", "--search") { HelpName = "pattern", Description = "Search pattern" };
        Add(SearchOption);
        DetailedOption = new Option<bool>("--detailed") { Description = "Show detailed information on entries" };
        Add(DetailedOption);
    }

    protected override Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        foreach (var plugin in PluginStore.LoadAllRegistries())
        {
            string? search = parseResult.GetValue(SearchOption);
            Regex? re = search != null ? Common.GetFilterRegex(search, false, false) : null;
            foreach (var desc in plugin.GetToolDescriptions()
                         .Where(v => re?.IsMatch(v.Id.GetToolString()) ?? true))
            {
                string toolString = desc.Id.GetToolString();
                Common.PrintFormat(toolString, parseResult.GetValue(DetailedOption), () =>
                {
                    bool canFind = desc.Type.IsAssignableTo(typeof(IArtifactFindTool));
                    bool canList = desc.Type.IsAssignableTo(typeof(IArtifactListTool));
                    bool canDump = canList || desc.Type.IsAssignableTo(typeof(IArtifactDumpTool));
                    bool canSelect = desc.Type.IsAssignableTo(typeof(IArtifactToolSelector<string>));
                    List<string> capabilities = [];
                    if (canFind)
                    {
                        capabilities.Add("find");
                    }
                    if (canList)
                    {
                        capabilities.Add("list");
                    }
                    if (canDump)
                    {
                        capabilities.Add("arc");
                    }
                    if (canSelect)
                    {
                        capabilities.Add("select");
                    }
                    if (capabilities.Count == 0)
                    {
                        capabilities.Add("none");
                    }
                    var stringBuilder = new StringBuilder("Capabilities: ").AppendJoin(", ", capabilities);
                    bool isCore = desc.Type.GetCustomAttribute<CoreAttribute>() != null;
                    stringBuilder.AppendLine().Append("IsCore: ").Append(isCore ? "true" : "false");
                    if (!isCore)
                    {
                        string coreToolString = ArtifactToolIDUtil.CreateCoreToolString(desc.Type);
                        if (!string.Equals(toolString, coreToolString, StringComparison.InvariantCulture))
                        {
                            stringBuilder.AppendLine().Append("CoreTool: ").Append(coreToolString);
                        }
                    }
                    if (desc.Type.GetCustomAttribute<ToolVersionAttribute>() is { } toolVersionAttribute)
                    {
                        stringBuilder.AppendLine().Append("ToolVersion: ").Append(toolVersionAttribute);
                    }
                    var assembly = desc.Type.Assembly;
                    if (assembly.GetCustomAttribute<AssemblyCopyrightAttribute>() is { } assemblyCopyrightAttribute)
                    {
                        stringBuilder.AppendLine().Append("PluginCopyright: ").Append(assemblyCopyrightAttribute.Copyright);
                    }
                    if (AssemblyAttributeUtility.GetAssemblyVersion(assembly) is { } assemblyVersion)
                    {
                        stringBuilder.AppendLine().Append("PluginVersion: ").Append(assemblyVersion);
                    }
                    foreach (var pair in desc.Properties)
                    {
                        stringBuilder.AppendLine().Append(pair.Key).Append(": ").Append(pair.Value);
                    }
                    return stringBuilder.ToString();
                }, ToolOutput);
            }
        }
        return Task.FromResult(0);
    }
}
