using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Art.Common;

namespace Art.Modular;

[RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
public class DynamicRegistryStore : IRegistryStore
{
    public IArtifactToolRegistry LoadRegistry(ArtifactToolID artifactToolId)
    {
        string assembly = artifactToolId.Assembly;
        if (!ModuleManifest.TryFind(assembly, out var manifest))
        {
            throw new ManifestNotFoundException(assembly);
        }
        return LoadForManifest(manifest);
    }

    private static IArtifactToolRegistry LoadForManifest(ModuleManifest manifest)
    {
        string baseDir = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path) ? Path.Combine(manifest.BasePath, manifest.Content.Path) : manifest.BasePath;
        var ctx = new ArtModuleAssemblyLoadContext(baseDir, manifest.Content.Assembly);
        return new Plugin(manifest, ctx, ctx.LoadFromAssemblyName(new AssemblyName(manifest.Content.Assembly)));
    }

    public IEnumerable<IArtifactToolRegistry> LoadAllRegistries()
    {
        var manifests = new Dictionary<string, ModuleManifest>();
        ModuleManifest.LoadManifests(manifests);
        foreach (ModuleManifest manifest in manifests.Values)
        {
            yield return LoadForManifest(manifest);
        }
    }
}
