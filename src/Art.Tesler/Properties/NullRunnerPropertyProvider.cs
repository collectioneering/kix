using System.Text.Json;

namespace Art.Tesler.Properties;

public class NullRunnerPropertyProvider : IRunnerPropertyProvider
{
    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties()
    {
        return Array.Empty<KeyValuePair<string, JsonElement>>();
    }

    public bool TryGetProperty(string key, out JsonElement value)
    {
        value = default;
        return false;
    }
}
