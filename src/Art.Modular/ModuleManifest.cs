using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Art.Modular;

public record ModuleManifest(string BasePath, ModuleManifestContent Content)
{
    private const string DirectorySuffix = ".kix";
    private const string FileNameSuffix = ".kix.json";
    private static readonly Dictionary<string, ModuleManifest> s_manifests = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly HashSet<string> s_searched = new();
    private static readonly string s_filePath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string s_pluginDirectory = Path.Combine(s_filePath, "Plugins");

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public static bool TryFind(string assembly, [NotNullWhen(true)] out ModuleManifest? manifest)
    {
        if (s_manifests.TryGetValue(assembly, out manifest))
        {
            return true;
        }
        if (!Directory.Exists(s_pluginDirectory))
        {
            manifest = null;
            return false;
        }
        if (TryFind(assembly, s_pluginDirectory, out manifest, s_manifests, s_searched))
        {
            return true;
        }
        manifest = null;
        return false;
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    public static void LoadManifests(IDictionary<string, ModuleManifest> dictionary)
    {
        if (!Directory.Exists(s_pluginDirectory)) return;
        LoadManifests(dictionary, s_pluginDirectory, s_searched);
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static bool TryFind(string assembly, string dir, [NotNullWhen(true)] out ModuleManifest? manifest, IDictionary<string, ModuleManifest>? toAugment = null, ISet<string>? searched = null)
    {
        foreach (string directory in Directory.EnumerateDirectories(dir, $"*{DirectorySuffix}", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
        {
            if (searched != null && !searched.Add(Path.GetFullPath(directory)))
            {
                continue;
            }
            if (TryFindAtTarget(assembly, directory, out manifest, toAugment))
            {
                return true;
            }
        }
        manifest = null;
        return false;
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static bool TryFindAtTarget(string assembly, string directory, [NotNullWhen(true)] out ModuleManifest? manifest, IDictionary<string, ModuleManifest>? toAugment = null)
    {
        foreach (string file in Directory.EnumerateFiles(directory, $"*{FileNameSuffix}", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
        {
            if (TryLoad(file, out var content))
            {
                manifest = null;
                if (toAugment != null && !toAugment.ContainsKey(content.Assembly))
                {
                    manifest = new ModuleManifest(directory, content);
                    toAugment.Add(content.Assembly, manifest);
                }
                if (content.Assembly.Equals(assembly, StringComparison.InvariantCultureIgnoreCase))
                {
                    manifest ??= new ModuleManifest(directory, content);
                    return true;
                }
            }
        }
        manifest = null;
        return false;
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static void LoadManifests(IDictionary<string, ModuleManifest> dictionary, string dir, ISet<string>? searched = null)
    {
        foreach (string directory in Directory.EnumerateDirectories(dir, $"*{DirectorySuffix}", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
        {
            if (searched != null && !searched.Add(Path.GetFullPath(directory)))
            {
                continue;
            }
            LoadManifestsAtTarget(dictionary, directory);
        }
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static void LoadManifestsAtTarget(IDictionary<string, ModuleManifest> dictionary, string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory, $"*{FileNameSuffix}", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
        {
            if (TryLoad(file, out var content) && !dictionary.ContainsKey(content.Assembly))
            {
                dictionary.Add(content.Assembly, new ModuleManifest(directory, content));
            }
        }
    }

    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static bool TryLoad(string file, [NotNullWhen(true)] out ModuleManifestContent? content)
    {
        try
        {
            content = JsonSerializer.Deserialize<ModuleManifestContent>(File.ReadAllText(file)) ?? throw new IOException($"Failed to deserialize manifest file {file}");
            return true;
        }
        catch
        {
            content = null;
            return false;
        }
    }
}
