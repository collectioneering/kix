using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Art;
using Art.Common;

namespace Kix;

internal record Plugin(KixManifest Manifest, KixAssemblyLoadContext Context, Assembly BaseAssembly)
{
    public bool TryLoadTool(ArtifactToolProfile artifactToolProfile, [NotNullWhen(true)] out IArtifactTool? t)
    {
        return ArtifactToolLoader.TryLoad(BaseAssembly, artifactToolProfile.Tool, out t);
    }

    public static Plugin LoadForToolString(string toolString)
    {
        (string assembly, _) = ArtifactToolProfileUtil.GetID(toolString);
        KixManifest manifest;
        try
        {
            manifest = KixManifest.GetManifests().Single(v => string.Equals(v.Content.Assembly, assembly, StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            throw new InvalidOperationException($"No applicable manifest for the assembly {assembly} could be found");
        }
        return LoadForManifest(manifest);
    }

    public static Plugin LoadForManifest(KixManifest manifest)
    {
        string baseDir = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path) ? Path.Combine(manifest.BasePath, manifest.Content.Path) : manifest.BasePath;
        ValidateArtVersion(baseDir, manifest.Content.Assembly);
        var ctx = new KixAssemblyLoadContext(baseDir);
        return new Plugin(manifest, ctx, ctx.LoadFromAssemblyName(new AssemblyName(manifest.Content.Assembly)));
    }

    private static void ValidateArtVersion(string basePath, string assemblyShortName)
    {
        var loadedAssemblyName = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Art")).GetName();
        if (loadedAssemblyName.GetPublicKeyToken() is not { } loadedPublicKeyToken)
        {
            throw new InvalidOperationException($"Active Art assembly [{loadedAssemblyName}] does not have a public key token");
        }
        // Art not referenced, don't treat as error for now
        if (!TryGetArtAssemblyName(basePath, assemblyShortName, out var assemblyName))
        {
            return;
        }
        if (assemblyName.GetPublicKeyToken() is not { } publicKeyToken)
        {
            throw new InvalidOperationException($"Plugin {assemblyShortName} at path {basePath} refers to Art assembly [{assemblyName}] that does not have a public key token");
        }
        if (!loadedPublicKeyToken.AsSpan().SequenceEqual(publicKeyToken))
        {
            throw new InvalidOperationException($"Plugin {assemblyShortName} at path {basePath} refers to Art assembly [{assemblyName}] that does not match public key token of active Art assembly [{loadedAssemblyName}]");
        }
    }

    private static bool TryGetArtAssemblyName(string basePath, string assemblyShortName, [NotNullWhen(true)] out AssemblyName? assemblyName)
    {
        List<string> paths = new();
        paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        paths.AddRange(Directory.GetFiles(basePath, "*.dll"));
        var resolver = new PathAssemblyResolver(paths);
        using var ctx = new MetadataLoadContext(resolver);
        var assembly = ctx.LoadFromAssemblyName(assemblyShortName);
        if (assembly.GetReferencedAssemblies().FirstOrDefault(v => v.Name == "Art") is { } artAssemblyName)
        {
            assemblyName = artAssemblyName;
            return true;
        }
        assemblyName = null;
        return false;
    }
}
