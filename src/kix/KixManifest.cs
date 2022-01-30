using System.Text.Json;

namespace kix;

internal record KixManifest(string BasePath, KixManifestContent Content)
{
    private const string FileNameSuffix = ".kix.json";
    private static readonly string s_filePath = AppDomain.CurrentDomain.BaseDirectory;
    private static List<KixManifest>? s_manifests;

    public static IReadOnlyCollection<KixManifest> GetManifests()
    {
        if (s_manifests != null) return s_manifests;
        s_manifests = new List<KixManifest>();
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(s_filePath))
            {
                foreach (string file in Directory.EnumerateFiles(dir).Where(f => f.EndsWith(FileNameSuffix, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        KixManifestContent content = JsonSerializer.Deserialize<KixManifestContent>(File.ReadAllText(file)) ?? throw new IOException();
                        s_manifests.Add(new KixManifest(dir, content));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        return s_manifests;
    }
}
