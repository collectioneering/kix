using System.Text.Json;
using Art.Common.IO;

namespace Art.Tesler.Properties;

public class FileJsonRunnerPropertyProvider : IWritableRunnerPropertyProvider
{
    private readonly string _propertyFile;

    public FileJsonRunnerPropertyProvider(string propertyFile)
    {
        _propertyFile = propertyFile;
    }

    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties()
    {
        if (File.Exists(_propertyFile) && JsonPropertyFileUtility.LoadPropertiesFromFile(_propertyFile) is { } map)
        {
            return map;
        }
        return Array.Empty<KeyValuePair<string, JsonElement>>();
    }

    public bool TryGetProperty(string key, out JsonElement value)
    {
        if (File.Exists(_propertyFile) && JsonPropertyFileUtility.LoadPropertiesFromFile(_propertyFile) is { } map)
        {
            return map.TryGetValue(key, out value);
        }
        value = default;
        return false;
    }

    public bool TrySetProperty(string key, JsonElement value)
    {
        Dictionary<string, JsonElement>? map;
        if (File.Exists(_propertyFile))
        {
            map = JsonPropertyFileUtility.LoadPropertiesFromFileWritable(_propertyFile);
        }
        else
        {
            map = null;
        }
        map = TeslerPropertyUtility.AddPairToOptionsMap(map, key, value, out bool toCreate);
        if (toCreate)
        {
            ArtIOUtility.EnsureDirectoryForFileCreated(_propertyFile);
        }
        JsonPropertyFileUtility.StorePropertiesToFile(_propertyFile, map);
        return true;
    }

    public bool TryUnsetProperty(string key)
    {
        Dictionary<string, JsonElement>? map;
        if (File.Exists(_propertyFile))
        {
            map = JsonPropertyFileUtility.LoadPropertiesFromFileWritable(_propertyFile);
        }
        else
        {
            return true;
        }
        TeslerPropertyUtility.RemoveKeyFromOptionsMap(map, key, out bool toDelete);
        if (toDelete)
        {
            File.Delete(_propertyFile);
            return true;
        }
        if (map != null)
        {
            JsonPropertyFileUtility.StorePropertiesToFile(_propertyFile, map);
        }
        return true;
    }
}
