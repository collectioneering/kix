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

    public static Plugin LoadForToolString(string toolString, bool enforceSharedAssemblyVersion)
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
        return LoadForManifest(manifest, enforceSharedAssemblyVersion);
    }

    public static Plugin LoadForManifest(KixManifest manifest, bool enforceSharedAssemblyVersion)
    {
        string baseDir = manifest.Content.Path != null && !Path.IsPathFullyQualified(manifest.Content.Path) ? Path.Combine(manifest.BasePath, manifest.Content.Path) : manifest.BasePath;
        if (enforceSharedAssemblyVersion)
        {
            ValidateArtVersion(baseDir, manifest.Content.Assembly);
        }
        var ctx = new KixAssemblyLoadContext(baseDir);
        return new Plugin(manifest, ctx, ctx.LoadFromAssemblyName(new AssemblyName(manifest.Content.Assembly)));
    }

    private static void ValidateArtVersion(string basePath, string assemblyShortName)
    {
        var loadedAssemblyName = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Art")).GetName();
        if (loadedAssemblyName.Version is not { } loadedVersion)
        {
            throw new InvalidOperationException($"""
                Loaded assembly [{loadedAssemblyName}] does not have a version.
                Assembly version validation cannot be performed.
                """);
        }
        // Art not referenced, don't treat as error for now
        if (!TryGetArtAssemblyName(basePath, assemblyShortName, out var assemblyName))
        {
            return;
        }
        if (assemblyName.Version is not { } version)
        {
            throw new InvalidOperationException($"""
                Plugin {assemblyShortName} at path {basePath} refers to assembly [{assemblyName}] that does not have a version.
                Assembly version validation cannot be performed.
                """);
        }
        if (!loadedVersion.Equals(version))
        {
            throw new InvalidOperationException($"""
                Plugin {assemblyShortName} at path {basePath} refers to assembly [{assemblyName}] that does not match version of loaded assembly [{loadedAssemblyName}].
                This indicates that the plugin was compiled with a reference to a different version of the library than the one this program references.
                This can lead to runtime errors if the API surface has changed.
                """);
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
