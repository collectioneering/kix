using System.Security.Cryptography;
using Art.Common;

namespace Art.Tesler;

public class ValidationContext : ToolControlContext
{
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed = new();
    private readonly IArtifactRegistrationManager _arm;
    private readonly IArtifactDataManager _adm;
    private readonly IToolLogHandler _l;

    public bool AnyFailed => _failed.Count != 0;

    public IEnumerable<KeyValuePair<ArtifactKey, int>> GetFailureCountsByKey() => _failed.Select(v => new KeyValuePair<ArtifactKey, int>(v.Key, v.Value.Count));

    public int CountResourceFailures() => _failed.Sum(v => v.Value.Count);

    public ValidationContext(IArtifactToolRegistryStore pluginStore, IArtifactRegistrationManager arm, IArtifactDataManager adm, IToolLogHandler l) : base(pluginStore)
    {
        _arm = arm;
        _adm = adm;
        _l = l;
    }

    private void AddFail(ArtifactResourceInfo r)
    {
        if (!_failed.TryGetValue(r.Key.Artifact, out var list)) list = _failed[r.Key.Artifact] = [];
        list.Add(r);
    }

    public async Task<ValidationProcessResult> ProcessAsync(IEnumerable<ArtifactInfo> artifacts, ChecksumSource? checksumSourceForAdd, CancellationToken cancellationToken)
    {
        int artifactCount = 0, resourceCount = 0;
        if (checksumSourceForAdd == null)
        {
            foreach (ArtifactInfo inf in artifacts)
            {
                var result = await ProcessAsync(inf, (ActiveHashAlgorithm?)null, cancellationToken).ConfigureAwait(false);
                artifactCount += result.Artifacts;
                resourceCount += result.Resources;
            }
        }
        else
        {
            using var hashAlgorithm = checksumSourceForAdd.CreateHashAlgorithm();
            foreach (ArtifactInfo inf in artifacts)
            {
                var result = await ProcessAsync(inf, new ActiveHashAlgorithm(checksumSourceForAdd.Id, hashAlgorithm), cancellationToken).ConfigureAwait(false);
                artifactCount += result.Artifacts;
                resourceCount += result.Resources;
            }
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public async Task<ValidationProcessResult> ProcessAsync(ArtifactInfo artifact, ChecksumSource? checksumSourceForAdd, CancellationToken cancellationToken)
    {
        ValidationProcessResult result;
        if (checksumSourceForAdd == null)
        {
            result = await ProcessAsync(artifact, (ActiveHashAlgorithm?)null, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var hashAlgorithm = checksumSourceForAdd.CreateHashAlgorithm();
            result = await ProcessAsync(artifact, new ActiveHashAlgorithm(checksumSourceForAdd.Id, hashAlgorithm), cancellationToken).ConfigureAwait(false);
        }
        return result;
    }

    private readonly record struct ActiveHashAlgorithm(string Id, HashAlgorithm HashAlgorithm);

    private async Task<ValidationProcessResult> ProcessAsync(ArtifactInfo artifact, ActiveHashAlgorithm? activeHashAlgorithmForAdd, CancellationToken cancellationToken)
    {
        int resourceCount = 0;
        foreach (ArtifactResourceInfo rInf in await _arm.ListResourcesAsync(artifact.Key, cancellationToken).ConfigureAwait(false))
        {
            resourceCount++;
            if (!await _adm.ExistsAsync(rInf.Key, cancellationToken).ConfigureAwait(false))
            {
                AddFail(rInf);
                continue;
            }
            if (rInf.Checksum == null)
            {
                if (activeHashAlgorithmForAdd is not { } activeHashAlgorithmForAddReal)
                {
                    AddFail(rInf);
                }
                else
                {
                    Stream sourceStreamAdd = await _adm.OpenInputStreamAsync(rInf.Key, cancellationToken).ConfigureAwait(false);
                    await using var streamAdd = sourceStreamAdd.ConfigureAwait(false);
                    byte[] newHash = await activeHashAlgorithmForAddReal.HashAlgorithm.ComputeHashAsync(sourceStreamAdd, cancellationToken).ConfigureAwait(false);
                    await _arm.AddResourceAsync(rInf with { Checksum = new Checksum(activeHashAlgorithmForAddReal.Id, newHash) }, cancellationToken).ConfigureAwait(false);
                }
                continue;
            }
            if (!ChecksumSource.DefaultSources.TryGetValue(rInf.Checksum.Id, out ChecksumSource? checksumSource))
            {
                AddFail(rInf);
                continue;
            }
            using var hashAlgorithm = checksumSource.CreateHashAlgorithm();
            Stream sourceStream = await _adm.OpenInputStreamAsync(rInf.Key, cancellationToken).ConfigureAwait(false);
            await using var stream = sourceStream.ConfigureAwait(false);
            byte[] existingHash = await hashAlgorithm.ComputeHashAsync(sourceStream, cancellationToken).ConfigureAwait(false);
            if (!rInf.Checksum.Value.AsSpan().SequenceEqual(existingHash)) AddFail(rInf);
        }
        return new ValidationProcessResult(1, resourceCount);
    }

    public async Task<ValidationProcessResult> ProcessAsync(IEnumerable<ArtifactToolProfile> profiles, ChecksumSource? checksumSourceForAdd, CancellationToken cancellationToken)
    {
        int artifactCount = 0, resourceCount = 0;
        foreach (ArtifactToolProfile originalProfile in profiles)
        {
            string toolName, group;
            bool isFindTool;
            using (var tool = LoadTool(originalProfile))
            {
                var actualProfile = originalProfile.WithCoreTool(tool);
                toolName = actualProfile.Tool;
                group = actualProfile.GetGroupOrFallback(tool.GroupFallback);
                isFindTool = tool is IArtifactFindTool;
            }
            _l.Log($"Processing entries for profile {toolName}/{group}", null, LogLevel.Title);
            var artifacts = await _arm.ListArtifactsAsync(toolName, group, cancellationToken).ConfigureAwait(false);
            // respect profile's artifact list
            // (checking against it being a find tool matches the behaviour of dump / list proxies)
            if (originalProfile.Options.TryGetOption("artifactList", out string[]? artifactList, SourceGenerationContext.s_context.StringArray) && isFindTool)
            {
                var set = artifactList.ToHashSet();
                artifacts.RemoveAll(v => !set.Contains(v.Key.Id));
            }
            var result = await ProcessAsync(artifacts, checksumSourceForAdd, cancellationToken).ConfigureAwait(false);
            _l.Log($"Processed {result.Artifacts} artifacts and {result.Resources} resources for profile {toolName}/{group}", null, LogLevel.Information);
            artifactCount += result.Artifacts;
            resourceCount += result.Resources;
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public RepairContext CreateRepairContext()
    {
        return new RepairContext(PluginStore, _failed, _arm, _adm, _l);
    }
}

public readonly record struct ValidationProcessResult(int Artifacts, int Resources);
