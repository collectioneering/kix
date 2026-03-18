using System.Text.Json;
using Art.Tesler.Properties;

namespace Art.Tesler.Tests;

internal class InMemoryToolPropertyProvider : IToolPropertyProvider
{
    private readonly IReadOnlyDictionary<string, JsonElement> _shared;
    private readonly IReadOnlyDictionary<ArtifactToolID, IReadOnlyDictionary<string, JsonElement>> _perTool;

    public InMemoryToolPropertyProvider(IReadOnlyDictionary<string, JsonElement> shared, IReadOnlyDictionary<ArtifactToolID, IReadOnlyDictionary<string, JsonElement>> perTool)
    {
        _shared = shared;
        _perTool = perTool;
    }

    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties(ArtifactToolID artifactToolId)
    {
        if (_perTool.TryGetValue(artifactToolId, out var dict))
        {
            return _shared.Concat(dict);
        }
        else
        {
            return _shared;
        }
    }

    public bool TryGetProperty(ArtifactToolID artifactToolId, string key, out JsonElement value)
    {
        if (_perTool.TryGetValue(artifactToolId, out var dict))
        {
            if (dict.TryGetValue(key, out value))
            {
                return true;
            }
        }
        return _shared.TryGetValue(key, out value);
    }
}
