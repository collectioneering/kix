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
        if (!KixManifest.TryFind(assembly, out var manifest))
        {
            throw new ManifestNotFoundException(assembly);
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
