using Art;

namespace KixSample;

public record FileArtifactResourceInfo(
    FileInfo Resource,
    ArtifactResourceKey Key,
    string? ContentType = "application/octet-stream",
    DateTimeOffset? Updated = null,
    DateTimeOffset? Retrieved = null,
    string? Version = null,
    Checksum? Checksum = null)
    : ArtifactResourceInfo(Key, ContentType, Updated, Retrieved, Version, Checksum)
{
    public override bool CanExportStream => Resource.Exists;

    public override bool CanGetStream => Resource.Exists;

    public override async ValueTask ExportStreamAsync(Stream targetStream, ArtifactResourceExportOptions? exportOptions = null, CancellationToken cancellationToken = default)
    {
        await using var fileStream = Resource.OpenRead();
        await fileStream.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
    }

    public override ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<Stream>(Resource.OpenRead());
    }

    /// <inheritdoc />
    public override void AugmentOutputStreamOptions(ref OutputStreamOptions options)
    {
        if (Resource.Exists)
        {
            options = options with { PreallocationSize = Resource.Length };
        }
    }

    public override ValueTask<ArtifactResourceInfo> WithMetadataAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<ArtifactResourceInfo>(Resource.Exists ? this with { Updated = Resource.LastWriteTime } : this);
    }
}
