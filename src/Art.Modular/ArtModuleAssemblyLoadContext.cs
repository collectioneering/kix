using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

namespace Art.Modular;

public class ArtModuleAssemblyLoadContext : AssemblyLoadContext
{
    // Need to share the core library so everyone uses the same Assembly instance and interface types from that instance
    private static readonly ImmutableHashSet<string> s_shared = new HashSet<string> { "Art" }.ToImmutableHashSet();
    public readonly string BasePath;

    private readonly AssemblyDependencyResolver _resolver;

    public ArtModuleAssemblyLoadContext(string basePath, string assembly)
    {
        BasePath = basePath;
        _resolver = new AssemblyDependencyResolver(Path.Combine(basePath, assembly + ".dll"));
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string name = assemblyName.Name!;
        if (s_shared.Contains(name))
        {
            return null;
        }
        string? asmPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (asmPath != null)
        {
            return LoadFromAssemblyPath(asmPath);
        }
        return null;
    }
}
