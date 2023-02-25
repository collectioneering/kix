using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Art.Common;

namespace Art.Modular;

[RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
public record Plugin(ModuleManifest Manifest, ArtModuleAssemblyLoadContext Context, Assembly BaseAssembly) : IArtifactToolRegistry
{
    public bool Contains(ArtifactToolID artifactToolId)
    {
        try
        {
            Assembly assembly = Context.LoadFromAssemblyName(new AssemblyName(artifactToolId.Assembly));
            return assembly.GetType(artifactToolId.Type) != null;
        }
        catch
        {
            return false;
        }
    }

    public bool TryLoad(ArtifactToolID artifactToolId, [NotNullWhen(true)] out IArtifactTool? tool)
    {
        return ArtifactToolLoader.TryLoad(Context, artifactToolId, out tool);
    }

    public IEnumerable<ToolDescription> GetToolDescriptions()
    {
        return BaseAssembly.GetExportedTypes()
            .Where(t => t.IsAssignableTo(typeof(IArtifactTool)) && !t.IsAbstract && t.GetConstructor(Array.Empty<Type>()) != null)
            .Select(v => new ToolDescription(v, ArtifactToolIDUtil.CreateToolId(v)));
    }
}
