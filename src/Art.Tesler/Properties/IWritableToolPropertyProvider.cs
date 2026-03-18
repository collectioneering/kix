using System.Text.Json;

namespace Art.Tesler.Properties;

public interface IWritableToolPropertyProvider : IToolPropertyProvider
{
    /// <summary>
    /// Attempts to set property for the specified key.
    /// </summary>
    /// <param name="artifactToolId">ID of tool to set property for.</param>
    /// <param name="key">Property key.</param>
    /// <param name="value">Property value.</param>
    /// <returns>True if successful.</returns>
    public bool TrySetProperty(ArtifactToolID artifactToolId, string key, JsonElement value);

    /// <summary>
    /// Attempts to unset property for the specified key.
    /// </summary>
    /// <param name="artifactToolId">ID of tool to unset property for.</param>
    /// <param name="key">Property key.</param>
    /// <returns>True if successful.</returns>
    public bool TryUnsetProperty(ArtifactToolID artifactToolId, string key);
}
