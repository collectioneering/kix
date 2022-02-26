using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Art;
using CommandLine;

namespace Kix;

[Verb("tools", HelpText = "List artifact tools.")]
internal class RunTools : IRunnable
{
    [Option('s', "search", HelpText = "Search pattern.", MetaValue = "pattern")]
    public string? Search { get; set; }

    [Option('v', "verbose", HelpText = "Enable verbose output.")]
    public bool Verbose { get; set; }

    [Option('d', "detailed", HelpText = "Show detailed information on entries.")]
    public bool Detailed { get; set; }

    public Task<int> RunAsync()
    {
        foreach (KixManifest manifest in KixManifest.GetManifests())
        {
            Assembly assembly;
            try
            {
                assembly = Common.LoadAssemblyForManifest(manifest);
            }
            catch (Exception ex)
            {
                if (Verbose) Console.WriteLine($"Failed to load assembly {manifest.Content.Assembly}:\n{ex}");
                continue;
            }
            if (Verbose) Console.WriteLine(assembly.FullName);
            Regex? re = Search != null ? Common.GetFilterRegex(Search, false, false) : null;
            foreach ((Type toolType, string toolString) in assembly.GetExportedTypes()
                         .Where(t => t.IsAssignableTo(typeof(ArtifactTool)) && !t.IsAbstract && t.GetConstructor(Array.Empty<Type>()) != null)
                         .Select(v => (ToolType: v, ToolString: ArtifactTool.CreateToolString(v)))
                         .Where(v => re?.IsMatch(v.ToolString) ?? true))
                Common.PrintFormat(toolString, Detailed, () =>
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
