using System.Reflection;
using System.Runtime.Loader;

namespace Art.Tesler;

public class CoreInfoLookup
{
    public static IReadOnlyList<string> DefaultSearchAssemblyNames => s_searchAssemblyNames;

    private static readonly string[] s_searchAssemblies =
    [
        "Artcore",
        "Art",
        "Art.BrowserCookies",
        "Art.Common",
        "Art.EF",
        "Art.EF.Sqlite",
        "Art.Html",
        "Art.Http",
        "Art.M3U",
        "Art.Modular",
        "Art.Tesler",
    ];

    private static readonly string[] s_searchAssemblyNames = s_searchAssemblies
        .Select(static assemblyName => $"{assemblyName}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
        .ToArray();

    private readonly string[] _assemblyNames;
    private readonly Assembly[] _assemblies;


    public CoreInfoLookup(IReadOnlyList<string>? assemblyNames, IReadOnlyList<Assembly>? assemblies)
    {
        _assemblyNames = assemblyNames?.ToArray() ?? s_searchAssemblies;
        _assemblies = assemblies?.ToArray() ?? [];
    }

    public IEnumerable<Assembly> GetAssemblies()
    {
        foreach (var assembly in _assemblies)
        {
            yield return assembly;
        }
        foreach (string assemblyName in _assemblyNames)
        {
            Assembly result;
            try
            {
                result = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
            }
            catch
            {
                continue;
            }
            yield return result;
        }
    }
}
