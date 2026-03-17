using Art;
using Art.Common;

namespace KixSample;

public static class SampleArtifactDataExtensions
{
    /// <summary>
    /// Creates a <see cref="FileArtifactResourceInfo"/> resource.
    /// </summary>
    /// <param name="artifactData">Source <see cref="ArtifactData"/> instance.</param>
    /// <param name="resource">Resource.</param>
    /// <param name="key">Resource key.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="updated">Date this resource was updated.</param>
    /// <param name="retrieved">Date this resource was retrieved.</param>
    /// <param name="version">Version.</param>
    /// <param name="checksum">Checksum.</param>
    public static ArtifactDataResource File(
        this ArtifactData artifactData,
        FileInfo resource,
        ArtifactResourceKey key,
        string? contentType = "",
        DateTimeOffset? updated = null,
        DateTimeOffset? retrieved = null,
        string? version = null,
        Checksum? checksum = null)
        => new(artifactData, new FileArtifactResourceInfo(resource, key, contentType, updated, retrieved, version, checksum));

    /// <summary>
    /// Creates a <see cref="FileArtifactResourceInfo"/> resource.
    /// </summary>
    /// <param name="artifactData">Source <see cref="ArtifactData"/> instance.</param>
    /// <param name="resource">Resource.</param>
    /// <param name="file">Filename.</param>
    /// <param name="path">Path.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="updated">Date this resource was updated.</param>
    /// <param name="retrieved">Date this resource was retrieved.</param>
    /// <param name="version">Version.</param>
    /// <param name="checksum">Checksum.</param>
    public static ArtifactDataResource File(
        this ArtifactData artifactData,
        FileInfo resource,
        string file,
        string path = "",
        string? contentType = "application/json",
        DateTimeOffset? updated = null,
        DateTimeOffset? retrieved = null,
        string? version = null,
        Checksum? checksum = null)
        => new(artifactData, new FileArtifactResourceInfo(resource, new ArtifactResourceKey(artifactData.Info.Key, file, path), contentType, updated, retrieved, version, checksum));
}
