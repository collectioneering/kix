// ReSharper disable CheckNamespace

#pragma warning disable CA1050

using System.Text.RegularExpressions;
using Art;
using Art.Common;
using KixSample;

[Core]
public partial class Disk : ArtifactTool, IArtifactFindTool, IArtifactToolSelfFactory<Disk>, IArtifactToolRegexSelector<Disk>
{
    [GeneratedRegex(@"^file://(?<path>[\S\s]+)$")]
    public static partial Regex GetArtifactToolSelectorRegex();

    public static string GetArtifactToolSelectorRegexIdGroupName() => "path";

    protected static string ResolveInputAsFilePath(string id)
    {
        return id.StartsWith("file://")
            ? new Uri(id) is { IsFile: true } uri
                ? uri.LocalPath
                : throw new ArgumentException($"Invalid ID {id}")
            : id;
    }

    public Task<IArtifactData?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IArtifactData?>(
            new FileInfo(ResolveInputAsFilePath(id)) is { Exists: true } fileInfo
                ? CreateArtifactDataForFileInfo(fileInfo)
                : null
        );
    }

    protected ArtifactData CreateArtifactDataForFileInfo(FileInfo fileInfo)
    {
        var data = CreateData(fileInfo.FullName, updateDate: fileInfo.Exists ? fileInfo.LastWriteTime : null);
        data.File(fileInfo, fileInfo.FullName).Commit(true);
        return data;
    }
}
