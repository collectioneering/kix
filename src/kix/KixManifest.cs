using System.Text.Json;

namespace Kix;

internal record KixManifest(string BasePath, KixManifestContent Content)
{
    private const string DirectorySuffix = ".kix";
    private const string FileNameSuffix = ".kix.json";
    private static readonly string s_filePath = AppDomain.CurrentDomain.BaseDirectory;
    private static List<KixManifest>? s_manifests;

    public static IReadOnlyCollection<KixManifest> GetManifests()
    {
        if (s_manifests != null) return s_manifests;
        s_manifests = new List<KixManifest>();
        foreach (string dir in Directory.EnumerateDirectories(s_filePath))
        {
            s_manifests.AddRange(LoadManifestsInFolder(dir));
            foreach (string dir2 in Directory.EnumerateDirectories(dir).Where(f => f.EndsWith(DirectorySuffix, StringComparison.InvariantCultureIgnoreCase)))
                s_manifests.AddRange(LoadManifestsInFolder(dir2));
        }
        return s_manifests;
    }

    private static IEnumerable<KixManifest> LoadManifestsInFolder(string dir)
    {
        foreach (string file in Directory.EnumerateFiles(dir).Where(f => f.EndsWith(FileNameSuffix, StringComparison.InvariantCultureIgnoreCase)))
        {
            KixManifestContent content;
            try
            {
                content = JsonSerializer.Deserialize<KixManifestContent>(File.ReadAllText(file)) ?? throw new IOException();
            }
            catch
            {
                continue;
            }
            yield return new KixManifest(dir, content);
        }
    }
}
