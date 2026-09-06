using System.Text.Json;

namespace Art.Tesler.Properties;

public static class JsonPropertyFileUtility
{
    internal static IReadOnlyDictionary<string, JsonElement>? LoadPropertiesFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadProperties(stream);
    }

    internal static ValueTask<IReadOnlyDictionary<string, JsonElement>?> LoadPropertiesFromFileAsync(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadPropertiesAsync(stream);
    }

    internal static Dictionary<string, JsonElement>? LoadPropertiesFromFileWritable(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadPropertiesWritable(stream);
    }

    internal static ValueTask<Dictionary<string, JsonElement>?> LoadPropertiesFromFileWritableAsync(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadPropertiesWritableAsync(stream);
    }

    internal static void StorePropertiesToFile(string path, IReadOnlyDictionary<string, JsonElement> properties)
    {
        using var stream = File.Create(path);
        StoreProperties(stream, properties);
    }

    internal static Task StorePropertiesToFileAsync(string path, IReadOnlyDictionary<string, JsonElement> properties)
    {
        using var stream = File.Create(path);
        return StorePropertiesAsync(stream, properties);
    }

    public static IReadOnlyDictionary<string, JsonElement>? LoadProperties(Stream stream)
    {
        return JsonSerializer.Deserialize(stream, SourceGenerationContext.SharedContext.IReadOnlyDictionaryStringJsonElement);
    }

    public static ValueTask<IReadOnlyDictionary<string, JsonElement>?> LoadPropertiesAsync(Stream stream)
    {
        return JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.SharedContext.IReadOnlyDictionaryStringJsonElement);
    }

    public static Dictionary<string, JsonElement>? LoadPropertiesWritable(Stream stream)
    {
        return JsonSerializer.Deserialize(stream, SourceGenerationContext.SharedContext.DictionaryStringJsonElement);
    }

    public static ValueTask<Dictionary<string, JsonElement>?> LoadPropertiesWritableAsync(Stream stream)
    {
        return JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.SharedContext.DictionaryStringJsonElement);
    }

    public static void StoreProperties(Stream stream, IReadOnlyDictionary<string, JsonElement> properties)
    {
        JsonSerializer.Serialize(stream, properties, SourceGenerationContext.SharedContext.IReadOnlyDictionaryStringJsonElement);
    }

    public static Task StorePropertiesAsync(Stream stream, IReadOnlyDictionary<string, JsonElement> properties)
    {
        return JsonSerializer.SerializeAsync(stream, properties, SourceGenerationContext.SharedContext.IReadOnlyDictionaryStringJsonElement);
    }
}