using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Art.Common;

namespace Art.Modular;

[RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
public record Plugin(ModuleManifest Manifest, ArtModuleAssemblyLoadContext Context, Assembly BaseAssembly) : IPlugin
{
    public IArtifactToolRegistry ArtifactToolRegistry => Context;

    public bool TryLoadTool(ArtifactToolProfile artifactToolProfile, [NotNullWhen(true)] out IArtifactTool? t)
    {
        return ArtifactToolLoader.TryLoad(BaseAssembly, artifactToolProfile.Tool, out t);
    }

    public IEnumerable<ToolDescription> GetToolDescriptions()
    {
        return BaseAssembly.GetExportedTypes()
            .Where(t => t.IsAssignableTo(typeof(IArtifactTool)) && !t.IsAbstract && t.GetConstructor(Array.Empty<Type>()) != null)
            .Select(v => new ToolDescription(v, ArtifactToolIdUtil.CreateToolId(v)));
    }
}
