// ReSharper disable CheckNamespace

#pragma warning disable CA1050

using System.Runtime.CompilerServices;
using Art;
using Art.Common;

public sealed class DiskList : Disk, IArtifactListTool, IArtifactToolSelfFactory<DiskList>
{
    static IArtifactTool IArtifactToolFactory.CreateArtifactTool() => new DiskList();

    static Type IArtifactToolFactory.GetArtifactToolType() => typeof(DiskList);

    static ArtifactToolID IArtifactToolFactory.GetArtifactToolId() => ArtifactToolIDUtil.CreateToolID<DiskList>();

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
            if (!dequeued.Exists)
            {
                continue;
            }
            switch (dequeued)
            {
                case DirectoryInfo directoryInfo:
                    foreach (var subInfo in directoryInfo.GetFileSystemInfos())
                    {
                        queue.Enqueue(subInfo);
                    }
                    await Task.Yield();
                    break;
                case FileInfo fileInfo:
                    yield return CreateArtifactDataForFileInfo(fileInfo);
                    break;
            }
        }
    }
}
