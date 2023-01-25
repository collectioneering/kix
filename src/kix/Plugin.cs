using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Art;
using Art.Common;

namespace kix;

internal record Plugin(KixManifest Manifest, KixAssemblyLoadContext Context, Assembly BaseAssembly)
{
    public bool TryLoadTool(ArtifactToolProfile artifactToolProfile, [NotNullWhen(true)] out IArtifactTool? t)
    {
        return ArtifactToolLoader.TryLoad(BaseAssembly, artifactToolProfile.Tool, out t);
    }

    public static Plugin LoadForToolString(string toolString)
    {
        string assembly = ArtifactToolProfileUtil.GetID(toolString).Assembly;
        if (!KixManifest.TryFind(assembly, out var manifest))
        {
            throw new InvalidOperationException($"No applicable manifest for the assembly {assembly} could be found");
        }
        return LoadForManifest(manifest);
    }

    public static Plugin LoadForManifest(KixManifest manifest)
    {
        string baseDir = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path) ? Path.Combine(manifest.BasePath, manifest.Content.Path) : manifest.BasePath;
        var ctx = new KixAssemblyLoadContext(baseDir);
        return new Plugin(manifest, ctx, ctx.LoadFromAssemblyName(new AssemblyName(manifest.Content.Assembly)));
    }
}
