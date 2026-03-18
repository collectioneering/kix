using System.Text.Json;

namespace Art.Tesler.Properties;

public class NullToolPropertyProvider : IToolPropertyProvider
{
    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties(ArtifactToolID artifactToolId)
    {
        return Array.Empty<KeyValuePair<string, JsonElement>>();
    }

    public bool TryGetProperty(ArtifactToolID artifactToolId, string key, out JsonElement value)
    {
        value = default;
        return false;
    }
}
