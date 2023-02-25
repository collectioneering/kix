using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Art.Common;

namespace Art.Modular;

[RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
public class PluginStore : IPluginStore
{
    public IPlugin LoadPluginFromToolString(string toolString)
    {
        ArtifactToolID id;
        try
        {
            id = ArtifactToolProfileUtil.GetID(toolString);
        }
        catch (ArgumentException e)
        {
            throw new ArtUserException(e.Message);
        }
        string assembly = id.Assembly;
        if (!ModuleManifest.TryFind(assembly, out var manifest))
        {
            throw new ManifestNotFoundException(assembly);
        }
        return LoadForManifest(manifest);
    }

    public IPlugin LoadPluginFromDescription(IPluginDescription pluginDescription)
    {
        if (pluginDescription is not PluginDescription desc)
        {
            throw new ArgumentException("Unsupported plugin description type for this store", nameof(pluginDescription));
        }
        return LoadForManifest(desc.Manifest);
    }

    private static IPlugin LoadForManifest(ModuleManifest manifest)
    {
        string baseDir = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path) ? Path.Combine(manifest.BasePath, manifest.Content.Path) : manifest.BasePath;
        var ctx = new ArtModuleAssemblyLoadContext(baseDir, manifest.Content.Assembly);
        return new Plugin(manifest, ctx, ctx.LoadFromAssemblyName(new AssemblyName(manifest.Content.Assembly)));
    }

    public IEnumerable<IPluginDescription> GetPluginDescriptions()
    {
        var manifests = new Dictionary<string, ModuleManifest>();
        ModuleManifest.LoadManifests(manifests);
        foreach (ModuleManifest manifest in manifests.Values)
        {
            yield return new PluginDescription(manifest);
        }
    }

    private class PluginDescription : IPluginDescription
    {
        public ModuleManifest Manifest { get; }

        public PluginDescription(ModuleManifest manifest) => Manifest = manifest;

        public string Name => Manifest.Content.Assembly;
    }
}
