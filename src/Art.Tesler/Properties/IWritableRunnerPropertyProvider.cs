using System.Text.Json;

namespace Art.Tesler.Properties;

public interface IWritableRunnerPropertyProvider : IRunnerPropertyProvider
{
    /// <summary>
    /// Attempts to set property for the specified key.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <param name="value">Property value.</param>
    /// <returns>True if successful.</returns>
    public bool TrySetProperty(string key, JsonElement value);

    /// <summary>
    /// Attempts to unset property for the specified key.
    /// </summary>
    /// <param name="key">Property key.</param>
    /// <returns>True if successful.</returns>
    public bool TryUnsetProperty(string key);
}
