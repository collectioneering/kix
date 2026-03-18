using System.Text.Json;
using Art.Common;
using Art.Common.IO;

namespace Art.Tesler.Properties;

public class DirectoryJsonToolPropertyProvider : IWritableToolPropertyProvider
{
    private readonly string _directory;
    private readonly Func<ArtifactToolID, string> _fileNameTransform;

    public DirectoryJsonToolPropertyProvider(string directory, Func<ArtifactToolID, string> fileNameTransform)
    {
        _directory = directory;
        _fileNameTransform = fileNameTransform;
    }

    public string GetPropertyFilePath(ArtifactToolID artifactToolId)
    {
        return Path.Join(_directory, _fileNameTransform(artifactToolId));
    }

    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties(ArtifactToolID artifactToolId)
    {
        string propertyFilePath = GetPropertyFilePath(artifactToolId);
        if (File.Exists(propertyFilePath) && JsonPropertyFileUtility.LoadPropertiesFromFile(propertyFilePath) is { } map)
        {
            return map;
        }
        return Array.Empty<KeyValuePair<string, JsonElement>>();
    }

    public bool TryGetProperty(ArtifactToolID artifactToolId, string key, out JsonElement value)
    {
        string propertyFilePath = GetPropertyFilePath(artifactToolId);
        if (File.Exists(propertyFilePath) && JsonPropertyFileUtility.LoadPropertiesFromFile(propertyFilePath) is { } map)
        {
            return map.TryGetValue(key, out value);
        }
        value = default;
        return false;
    }

    public static string DefaultFileNameTransform(ArtifactToolID artifactToolId)
    {
        string toolNameSafe = artifactToolId.GetToolString().SafeifyFileName();
        return $"toolconfig-{toolNameSafe}.json";
    }

    public bool TrySetProperty(ArtifactToolID artifactToolId, string key, JsonElement value)
    {
        string propertyFilePath = GetPropertyFilePath(artifactToolId);
        Dictionary<string, JsonElement>? map;
        if (File.Exists(propertyFilePath))
        {
            map = JsonPropertyFileUtility.LoadPropertiesFromFileWritable(propertyFilePath);
        }
        else
        {
            map = null;
        }
        map = TeslerPropertyUtility.AddPairToOptionsMap(map, key, value, out bool toCreate);
        if (toCreate)
        {
            ArtIOUtility.EnsureDirectoryForFileCreated(propertyFilePath);
        }
        JsonPropertyFileUtility.StorePropertiesToFile(propertyFilePath, map);
        return true;
    }

    public bool TryUnsetProperty(ArtifactToolID artifactToolId, string key)
    {
        string propertyFilePath = GetPropertyFilePath(artifactToolId);
        Dictionary<string, JsonElement>? map;
        if (File.Exists(propertyFilePath))
        {
            map = JsonPropertyFileUtility.LoadPropertiesFromFileWritable(propertyFilePath);
        }
        else
        {
            return true;
        }
        TeslerPropertyUtility.RemoveKeyFromOptionsMap(map, key, out bool toDelete);
        if (toDelete)
        {
            File.Delete(propertyFilePath);
            return true;
        }
        if (map != null)
        {
            JsonPropertyFileUtility.StorePropertiesToFile(propertyFilePath, map);
        }
        return true;
    }
}
