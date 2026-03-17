// ReSharper disable CheckNamespace

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Art;
using Art.Common;

[Core]
public partial class Disk : ArtifactTool, IArtifactFindTool, IArtifactToolSelfFactory<Disk>, IArtifactToolRegexSelector<Disk>
{
    [GeneratedRegex(@"^file\://(?<path>[\S\s]+)$")]
    public static partial Regex GetArtifactToolSelectorRegex();

    public static string GetArtifactToolSelectorRegexIdGroupName() => "path";

    protected string ResolveInputAsFilePath(string id)
    {
        if (!id.StartsWith("file://"))
        {
            return id;
        }
        Uri uri = new(id);
        if (!uri.IsFile)
        {
            throw new ArgumentException($"Invalid ID {id}");
        }
        return uri.LocalPath;
    }

    public Task<IArtifactData?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        id = ResolveInputAsFilePath(id);
        FileInfo fileInfo = new(id);
        if (!fileInfo.Exists)
        {
            return Task.FromResult<IArtifactData?>(null);
        }
        return Task.FromResult<IArtifactData?>(CreateArtifactDataForFileInfo(fileInfo));
    }

    protected ArtifactData CreateArtifactDataForFileInfo(FileInfo fileInfo)
    {
        var data = CreateData(fileInfo.FullName, updateDate: fileInfo.Exists ? fileInfo.LastWriteTime : null);
        data.Add(new FileArtifactResourceInfo(fileInfo, new ArtifactResourceKey(data.Info.Key, fileInfo.FullName)));
        return data;
    }
}

public sealed class DiskList : Disk, IArtifactListTool
{
    public const string BaseDirectoryOptionName = "BaseDirectory";
    public string BaseDirectory { get; set; } = null!;

    public override async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        await base.ConfigureAsync(cancellationToken);
        BaseDirectory = GetOptionToStringOrGroup(BaseDirectoryOptionName);
    }

    public async IAsyncEnumerable<IArtifactData> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Queue<FileSystemInfo> queue = new([new DirectoryInfo(ResolveInputAsFilePath(BaseDirectory))]);
        while (queue.TryDequeue(out var dequeued))
        {
            switch (dequeued)
            {
                case DirectoryInfo directoryInfo:
                    if (!directoryInfo.Exists)
                    {
                        continue;
                    }
                    foreach (var subInfo in directoryInfo.GetFileSystemInfos())
                    {
                        queue.Enqueue(subInfo);
                    }
                    await Task.Yield();
                    break;
                case FileInfo fileInfo:
                    if (!fileInfo.Exists)
                    {
                        continue;
                    }
                    yield return CreateArtifactDataForFileInfo(fileInfo);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dequeued));
            }
        }
    }
}

internal record FileArtifactResourceInfo(
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

    public override bool CanGetStream
    {
        get { return Resource.Exists; }
    }

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
        if (Resource.Exists) options = options with { PreallocationSize = Resource.Length };
    }

    public override ValueTask<ArtifactResourceInfo> WithMetadataAsync(CancellationToken cancellationToken = default)
    {
        if (!Resource.Exists)
        {
            return ValueTask.FromResult<ArtifactResourceInfo>(this);
        }
        return ValueTask.FromResult<ArtifactResourceInfo>(this with { Updated = Resource.LastWriteTime });
    }
}
