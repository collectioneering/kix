using System.Text.Json;

namespace Art.Tesler.Properties;

public interface IRunnerPropertyProvider
{
    /// <summary>
    /// Gets properties. Pairs returned later override earlier values for the same key.
    /// </summary>
    /// <returns>Sequence of key-value pairs for configuration.</returns>
    IEnumerable<KeyValuePair<string, JsonElement>> GetProperties();

    /// <summary>
    /// Attempts to get property with the specified key.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <param name="value">Resolved property value if successful.</param>
    /// <returns>True if successful.</returns>
    public bool TryGetProperty(string key, out JsonElement value);
}
