using System.Reflection;

namespace Art.Tesler;

internal static class AssemblyAttributeUtility
{
    public static string? GetAssemblyVersion(Assembly assembly)
    {
        if (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() is { } assemblyInformationalVersionAttribute)
        {
            return assemblyInformationalVersionAttribute.InformationalVersion;
        }
        if (assembly.GetCustomAttribute<AssemblyFileVersionAttribute>() is { } assemblyFileVersionAttribute)
        {
            return assemblyFileVersionAttribute.Version;
        }
        return null;
    }
}
