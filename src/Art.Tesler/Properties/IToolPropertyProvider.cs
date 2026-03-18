using System.Text.Json;

namespace Art.Tesler.Properties;

public interface IToolPropertyProvider
{
    /// <summary>
    /// Enumerates properties. Pairs returned later override earlier values for the same key.
    /// </summary>
    /// <param name="artifactToolId">ID of tool to get properties for.</param>
    /// <returns>Sequence of key-value pairs for configuration.</returns>
    IEnumerable<KeyValuePair<string, JsonElement>> GetProperties(ArtifactToolID artifactToolId);

    /// <summary>
    /// Attempts to get property with the specified key.
    /// </summary>
    /// <param name="artifactToolId">ID of tool to get property for.</param>
    /// <param name="key">Property key.</param>
    /// <param name="value">Resolved property value if successful.</param>
    /// <returns>True if successful.</returns>
    public bool TryGetProperty(ArtifactToolID artifactToolId, string key, out JsonElement value);
}
