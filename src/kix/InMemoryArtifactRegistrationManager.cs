using System.Linq.Expressions;
using Art;

namespace kix;

public class InMemoryArtifactRegistrationManager : ArtifactRegistrationManager
{
    public IReadOnlySet<ArtifactInfo> CheckedArtifacts => _checkedArtifacts;

    private readonly HashSet<ArtifactInfo> _checkedArtifacts = new();

    public IReadOnlySet<ArtifactKey> CheckedIds => _checkedIds;

    private readonly HashSet<ArtifactKey> _checkedIds = new();

    public IReadOnlyDictionary<ArtifactKey, ArtifactInfo> Artifacts => _artifacts;

    private readonly Dictionary<ArtifactKey, ArtifactInfo> _artifacts = new();

    public IReadOnlyDictionary<ArtifactResourceKey, ArtifactResourceInfo> Resources => _resources;

    private readonly Dictionary<ArtifactResourceKey, ArtifactResourceInfo> _resources = new();

    public override Task<List<ArtifactInfo>> ListArtifactsAsync(CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public override Task<List<ArtifactInfo>> ListArtifactsAsync(Expression<Func<ArtifactInfoModel, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task<List<ArtifactInfo>> ListArtifactsAsync(string tool, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public override Task<List<ArtifactInfo>> ListArtifactsAsync(string tool, string @group, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public override Task<List<ArtifactResourceInfo>> ListResourcesAsync(ArtifactKey key, CancellationToken cancellationToken = new()) => throw new NotImplementedException();

    public override ValueTask AddArtifactAsync(ArtifactInfo artifactInfo, CancellationToken ct = default)
    {
        _checkedArtifacts.Add(artifactInfo);
        _checkedIds.Add(artifactInfo.Key);
        _artifacts[artifactInfo.Key] = artifactInfo;
        return ValueTask.CompletedTask;
    }

    public override ValueTask<ArtifactInfo?> TryGetArtifactAsync(ArtifactKey key, CancellationToken ct = default)
    {
        _checkedIds.Add(key);
        return new ValueTask<ArtifactInfo?>(_artifacts.TryGetValue(key, out ArtifactInfo? value) ? value : null);
    }

    public override ValueTask AddResourceAsync(ArtifactResourceInfo artifactResourceInfo, CancellationToken ct = default)
    {
        _resources.Add(artifactResourceInfo.Key, artifactResourceInfo);
        return ValueTask.CompletedTask;
    }

    public override ValueTask<ArtifactResourceInfo?> TryGetResourceAsync(ArtifactResourceKey key, CancellationToken ct = default)
    {
        return new ValueTask<ArtifactResourceInfo?>(_resources.TryGetValue(key, out ArtifactResourceInfo? value) ? value : null);
    }

    public override ValueTask RemoveArtifactAsync(ArtifactKey key, CancellationToken cancellationToken = default)
    {
        _checkedIds.Add(key);
        _artifacts.Remove(key);
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveResourceAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
    {
        _resources.Remove(key);
        return ValueTask.CompletedTask;
    }
}
