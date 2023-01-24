using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

namespace Kix;

public class KixAssemblyLoadContext : AssemblyLoadContext
{
    // Need to share the core library so everyone uses the same Assembly instance and interface types from that instance
    private static readonly ImmutableHashSet<string> s_shared = new HashSet<string> { "Art" }.ToImmutableHashSet();
    public readonly string BasePath;

    public KixAssemblyLoadContext(string basePath)
    {
        BasePath = basePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string name = assemblyName.Name!;
        if (s_shared.Contains(name))
        {
            return null;
        }
        try
        {
            return LoadFromAssemblyPath(Path.Combine(BasePath, $"{assemblyName.Name!}.dll"));
        }
        catch
        {
            return null;
        }
    }
}
