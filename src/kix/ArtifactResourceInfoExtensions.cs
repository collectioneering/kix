using Art;

namespace kix;

// TODO expose the ones in Art.Common
internal static class ArtifactResourceInfoExtensions
{
    /// <summary>
    /// Checks if non-identifying metadata (i.e. everything but key, updated date, version) is different.
    /// </summary>
    /// <param name="artifactResourceInfo">Self.</param>
    /// <param name="other">Resource to compare to.</param>
    /// <returns>True if any metadata is different or if other is null.</returns>
    public static bool IsMetadataDifferent(this ArtifactResourceInfo artifactResourceInfo, ArtifactResourceInfo? other)
    {
        if (other == null) return true;
        return artifactResourceInfo.ContentType != other.ContentType;
    }

    /// <summary>
    /// Gets informational path string.
    /// </summary>
    /// <returns>Info path string.</returns>
    public static string GetInfoPathString(this ArtifactResourceInfo artifactResourceInfo) => $"{artifactResourceInfo.Key.Path}/{artifactResourceInfo.Key.File}";

    /// <summary>
    /// Gets informational string.
    /// </summary>
    /// <returns>Info string.</returns>
    public static string GetInfoString(this ArtifactResourceInfo artifactResourceInfo) => $"Path: {artifactResourceInfo.GetInfoPathString()}{(artifactResourceInfo.ContentType != null ? $"\nContent type: {artifactResourceInfo.ContentType}" : "")}{(artifactResourceInfo.Updated != null ? $"\nUpdated: {artifactResourceInfo.Updated}" : "")}{(artifactResourceInfo.Version != null ? $"\nVersion: {artifactResourceInfo.Version}" : "")}{(artifactResourceInfo.Checksum != null ? $"\nChecksum: {artifactResourceInfo.Checksum.Id}:{Convert.ToHexString(artifactResourceInfo.Checksum.Value)}" : "")}";
}
