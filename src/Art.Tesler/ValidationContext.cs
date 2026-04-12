using System.Security.Cryptography;
using Art.Common;

namespace Art.Tesler;

public class ValidationContext : ToolControlContext
{
    private static readonly Guid s_timedNamedProgressOperation = Guid.ParseExact("923b6d0747667facd296e1019dd7eae8", "N");

    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed = new();
    private readonly IArtifactRegistrationManager _arm;
    private readonly IArtifactDataManager _adm;
    private readonly IToolLogHandler _l;
    private readonly bool _progressMeter;

    public bool AnyFailed => _failed.Count != 0;

    public IEnumerable<KeyValuePair<ArtifactKey, int>> GetFailureCountsByKey() => _failed.Select(v => new KeyValuePair<ArtifactKey, int>(v.Key, v.Value.Count));

    public int CountResourceFailures() => _failed.Sum(v => v.Value.Count);

    public ValidationContext(
        IArtifactToolRegistryStore pluginStore,
        IArtifactRegistrationManager arm,
        IArtifactDataManager adm,
        IToolLogHandler l,
        bool progressMeter) : base(pluginStore)
    {
        _arm = arm;
        _adm = adm;
        _l = l;
        _progressMeter = progressMeter;
    }

    private void AddFail(ArtifactResourceInfo r)
    {
        if (!_failed.TryGetValue(r.Key.Artifact, out var list)) list = _failed[r.Key.Artifact] = [];
        list.Add(r);
    }

    public async Task<ValidationProcessResult> ProcessAsync(IReadOnlyCollection<ArtifactInfo> artifacts, ChecksumSource? checksumSourceForAdd, CancellationToken cancellationToken)
    {
        int artifactCount = 0, resourceCount = 0;
        var ndc = new NamedDisplayProgressCache(new NamedDisplayProgressCache.Parameters("Artifact", 0, artifacts.Count));
        var context = _progressMeter && _l.TryGetConcurrentOperationProgressContext(GetProgressName(), s_timedNamedProgressOperation, out var contextTmp) ? contextTmp : null;
        try
        {
            if (checksumSourceForAdd == null)
            {
                foreach (ArtifactInfo inf in artifacts)
                {
                    context?.ReportNamed((float)artifactCount / artifacts.Count, GetProgressName());
                    var result = await ProcessArtifactAsync(inf, null, context, cancellationToken).ConfigureAwait(false);
                    artifactCount += result.Artifacts;
                    resourceCount += result.Resources;
                }
            }
            else
            {
                using var hashAlgorithm = checksumSourceForAdd.CreateHashAlgorithm();
                foreach (ArtifactInfo inf in artifacts)
                {
                    context?.ReportNamed((float)artifactCount / artifacts.Count, GetProgressName());
                    var result = await ProcessArtifactAsync(inf, new ActiveHashAlgorithm(checksumSourceForAdd.Id, hashAlgorithm), context, cancellationToken).ConfigureAwait(false);
                    artifactCount += result.Artifacts;
                    resourceCount += result.Resources;
                }
            }
            context?.ReportNamed((float)artifactCount / artifacts.Count, GetProgressName());
            context?.MarkSafe();
        }
        finally
        {
            context?.Dispose();
        }
        return new ValidationProcessResult(artifactCount, resourceCount);

        // ReSharper disable AccessToModifiedClosure
        NamedDisplayProgressCache.Parameters UpdateProgressCacheParameters(NamedDisplayProgressCache.Parameters v) => v with { Numerator = artifactCount + 1 };
        // ReSharper restore AccessToModifiedClosure

        string GetProgressName()
        {
            // ReSharper disable AccessToModifiedClosure
            return ndc.GetString(UpdateProgressCacheParameters);
            // ReSharper restore AccessToModifiedClosure
        }
    }

    public async Task<ValidationProcessResult> ProcessArtifactAsync(ArtifactInfo artifact, ChecksumSource? checksumSourceForAdd, CancellationToken cancellationToken)
    {
        ValidationProcessResult result;
        if (checksumSourceForAdd == null)
        {
            result = await ProcessArtifactAsync(artifact, null, null, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var hashAlgorithm = checksumSourceForAdd.CreateHashAlgorithm();
            result = await ProcessArtifactAsync(artifact, new ActiveHashAlgorithm(checksumSourceForAdd.Id, hashAlgorithm), null, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }

    private readonly record struct ActiveHashAlgorithm(string Id, HashAlgorithm HashAlgorithm);

    private async Task<ValidationProcessResult> ProcessArtifactAsync(
        ArtifactInfo artifact,
        ActiveHashAlgorithm? activeHashAlgorithmForAdd,
        IOperationProgressContext? parentProgressContext,
        CancellationToken cancellationToken)
    {
        int resourceCount = 0;
        var resources = await _arm.ListResourcesAsync(artifact.Key, cancellationToken).ConfigureAwait(false);
        var ndc = new NamedDisplayProgressCache(new NamedDisplayProgressCache.Parameters("Resource", 0, resources.Count));
        var context = _progressMeter && _l.TryGetConcurrentOperationProgressContext(GetResourceProgressName(), s_timedNamedProgressOperation, out var contextTmp) ? contextTmp : null;
        try
        {
            foreach (ArtifactResourceInfo rInf in resources)
            {
                context?.ReportNamed((float)resourceCount / resources.Count, GetResourceProgressName());
                parentProgressContext?.Refresh();
                resourceCount++;
                if (!await _adm.ExistsAsync(rInf.Key, cancellationToken).ConfigureAwait(false))
                {
                    AddFail(rInf);
                    continue;
                }
                var key = rInf.Key;
                string resourceDisplayName = !string.IsNullOrEmpty(key.Path) ? $"{key.Path}/{key.File}" : key.File;
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
                        byte[] newHash;
                        var context2 = _progressMeter && _l.TryGetConcurrentOperationProgressContext(resourceDisplayName, s_timedNamedProgressOperation, out var context2Tmp) ? context2Tmp : null;
                        try
                        {
                            if (context2 != null)
                            {
                                long? contentLength = sourceStreamAdd.CanSeek ? sourceStreamAdd.Length : null;
                                newHash = await ReportUtility.ComputeHashWithReportAsync(sourceStreamAdd, activeHashAlgorithmForAddReal.HashAlgorithm, context2, parentProgressContext, contentLength, cancellationToken);
                                context2.MarkSafe();
                            }
                            else
                            {
                                newHash = await activeHashAlgorithmForAddReal.HashAlgorithm.ComputeHashAsync(sourceStreamAdd, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            context2?.Dispose();
                        }
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
                byte[] existingHash;
                var context3 = _progressMeter && _l.TryGetConcurrentOperationProgressContext(resourceDisplayName, s_timedNamedProgressOperation, out var context3Tmp) ? context3Tmp : null;
                try
                {
                    if (context3 != null)
                    {
                        long? contentLength = sourceStream.CanSeek ? sourceStream.Length : null;
                        existingHash = await ReportUtility.ComputeHashWithReportAsync(sourceStream, hashAlgorithm, context3, parentProgressContext, contentLength, cancellationToken);
                        context3.MarkSafe();
                    }
                    else
                    {
                        existingHash = await hashAlgorithm.ComputeHashAsync(sourceStream, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    context3?.Dispose();
                }
                if (!rInf.Checksum.Value.AsSpan().SequenceEqual(existingHash)) AddFail(rInf);
            }
            context?.ReportNamed((float)resourceCount / resources.Count, GetResourceProgressName());
            context?.MarkSafe();
            parentProgressContext?.Refresh();
        }
        finally
        {
            context?.Dispose();
        }
        return new ValidationProcessResult(1, resourceCount);

        // ReSharper disable AccessToModifiedClosure
        NamedDisplayProgressCache.Parameters UpdateResourceProgressCacheParameters(NamedDisplayProgressCache.Parameters v) => v with { Numerator = resourceCount + 1 };
        // ReSharper restore AccessToModifiedClosure

        string GetResourceProgressName()
        {
            return ndc.GetString(UpdateResourceProgressCacheParameters);
        }
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
            // respect profile's artifact list
            // (checking against it being a find tool matches the behaviour of dump / list proxies)
            HashSet<string>? set = null;
            if (originalProfile.Options.TryGetOption("artifactList", out string[]? artifactList, SourceGenerationContext.s_context.StringArray) && isFindTool)
            {
                set = artifactList.ToHashSet();
            }
            _l.Log($"Processing {(set != null ? $"{set.Count} " : "")}entries for profile {toolName}/{group}", null, LogLevel.Title);
            var artifacts = await _arm.ListArtifactsAsync(toolName, group, cancellationToken).ConfigureAwait(false);
            if (set != null)
            {
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
