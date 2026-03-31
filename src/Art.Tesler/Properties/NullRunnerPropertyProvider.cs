using System.Text.Json;

namespace Art.Tesler.Properties;

public class NullRunnerPropertyProvider : IRunnerPropertyProvider
{
    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties()
    {
        return [];
    }

    public bool TryGetProperty(string key, out JsonElement value)
    {
        value = default;
        return false;
    }
}
