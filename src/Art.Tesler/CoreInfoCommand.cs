using System.CommandLine;
using System.Text;

namespace Art.Tesler;

public class CoreInfoCommand : CommandBase
{
    private readonly CoreInfoLookup _lookup;
    public CoreInfoCommand(
        IOutputControl toolOutput,
        CoreInfoLookup lookup)
        : this(toolOutput, lookup, "coreinfo", "Show details about this program.")
    {
    }

    public CoreInfoCommand(
        IOutputControl toolOutput,
        CoreInfoLookup lookup,
        string name, string? description = null)
        : base(toolOutput, name, description)
    {
        _lookup = lookup;
    }

    protected override Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        foreach (var assembly in _lookup.GetAssemblies())
        {
            Common.PrintFormat(assembly.GetName().Name ?? "", true, () =>
            {
                var stringBuilder = new StringBuilder();
                if (AssemblyAttributeUtility.GetAssemblyVersion(assembly) is { } assemblyVersion)
                {
                    if (stringBuilder.Length != 0)
                    {
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append("Version: ").Append(assemblyVersion);
                }
                return stringBuilder.ToString();
            }, ToolOutput);
        }
        return Task.FromResult(0);
    }
}
