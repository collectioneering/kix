using System.Security.Cryptography;
using Art.Common;

namespace Art.Tesler;

public class ValidationContext<TPluginStore> where TPluginStore : IRegistryStore
{
    private readonly TPluginStore _pluginStore;
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed = new();
    private readonly IArtifactRegistrationManager _arm;
    private readonly ArtifactDataManager _adm;
    private readonly IToolLogHandler _l;

    public bool AnyFailed => _failed.Count != 0;

    public int CountResourceFailures() => _failed.Sum(v => v.Value.Count);

    public ValidationContext(TPluginStore pluginStore, IArtifactRegistrationManager arm, ArtifactDataManager adm, IToolLogHandler l)
    {
        _pluginStore = pluginStore;
        _arm = arm;
        _adm = adm;
        _l = l;
    }

    private void AddFail(ArtifactResourceInfo r)
    {
        if (!_failed.TryGetValue(r.Key.Artifact, out var list)) list = _failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
        list.Add(r);
    }

    public async Task<ValidationProcessResult> ProcessAsync(List<ArtifactInfo> artifacts, string? hashForAdd)
    {
        // TODO context should accept ChecksumSource
        int artifactCount = 0, resourceCount = 0;
        if (hashForAdd == null)
        {
            foreach (ArtifactInfo inf in artifacts)
            {
                var result = await ProcessAsync(inf, hashForAdd, null);
                artifactCount += result.Artifacts;
                resourceCount += result.Resources;
            }
        }
        else
        {
            if (!ChecksumSource.TryGetHashAlgorithm(hashForAdd, out HashAlgorithm? hashAlgorithm))
            {
                throw new ArgumentException($"Unsupported hash algorithm {hashForAdd}");
            }
            try
            {
                foreach (ArtifactInfo inf in artifacts)
                {
                    var result = await ProcessAsync(inf, hashForAdd, hashAlgorithm);
                    artifactCount += result.Artifacts;
                    resourceCount += result.Resources;
                }
            }
            finally
            {
                hashAlgorithm.Dispose();
            }
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public async Task<ValidationProcessResult> ProcessAsync(ArtifactInfo artifact, string? hashForAdd)
    {
        // TODO context should accept ChecksumSource
        ValidationProcessResult result;
        if (hashForAdd == null)
        {
            result = await ProcessAsync(artifact, hashForAdd, null);
        }
        else
        {
            if (!ChecksumSource.TryGetHashAlgorithm(hashForAdd, out HashAlgorithm? hashAlgorithm))
            {
                throw new ArgumentException($"Unsupported hash algorithm {hashForAdd}");
            }
            try
            {
                result = await ProcessAsync(artifact, hashForAdd, hashAlgorithm);
            }
            finally
            {
                hashAlgorithm.Dispose();
            }
        }
        return result;
    }

    private async Task<ValidationProcessResult> ProcessAsync(ArtifactInfo artifact, string? hashForAdd, HashAlgorithm? hashAlgorithmForAdd)
    {
        int resourceCount = 0;
        foreach (ArtifactResourceInfo rInf in await _arm.ListResourcesAsync(artifact.Key))
        {
            resourceCount++;
            if (!await _adm.ExistsAsync(rInf.Key))
            {
                AddFail(rInf);
                continue;
            }
            if (rInf.Checksum == null)
            {
                if (hashForAdd == null || hashAlgorithmForAdd == null)
                {
                    AddFail(rInf);
                }
                else
                {
                    await using Stream sourceStreamAdd = await _adm.OpenInputStreamAsync(rInf.Key);
                    byte[] hash = await hashAlgorithmForAdd.ComputeHashAsync(sourceStreamAdd);
                    await _arm.AddResourceAsync(rInf with { Checksum = new Checksum(hashForAdd, hash) });
                }
                continue;
            }
            if (!ChecksumSource.TryGetHashAlgorithm(rInf.Checksum.Id, out HashAlgorithm? hashAlgorithm))
            {
                AddFail(rInf);
                continue;
            }
            try
            {
                await using Stream sourceStream = await _adm.OpenInputStreamAsync(rInf.Key);
                byte[] hash = await hashAlgorithm.ComputeHashAsync(sourceStream);
                if (!rInf.Checksum.Value.AsSpan().SequenceEqual(hash)) AddFail(rInf);
            }
            finally
            {
                hashAlgorithm.Dispose();
            }
        }
        return new ValidationProcessResult(1, resourceCount);
    }

    public async Task<ValidationProcessResult> ProcessAsync(IEnumerable<ArtifactToolProfile> profiles, string? hashForAdd)
    {
        // TODO context should accept ChecksumSource
        int artifactCount = 0, resourceCount = 0;
        foreach (ArtifactToolProfile profile in profiles)
        {
            var context = _pluginStore.LoadRegistry(ArtifactToolProfileUtil.GetID(profile.Tool)); // InvalidOperationException
            if (!context.TryLoad(profile.GetID(), out var t))
                throw new InvalidOperationException($"Unknown tool {profile.Tool}");
            using IArtifactTool tool = t;
            var pp = profile.WithCoreTool(tool);
            _l.Log($"Processing entries for profile {pp.Tool}/{pp.Group}", null, LogLevel.Title);
            var result = await ProcessAsync(await _arm.ListArtifactsAsync(pp.Tool, pp.Group), hashForAdd);
            _l.Log($"Processed {result.Artifacts} artifacts and {result.Resources} resources for profile {pp.Tool}/{pp.Group}", null, LogLevel.Information);
            artifactCount += result.Artifacts;
            resourceCount += result.Resources;
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public RepairContext<TPluginStore> CreateRepairContext() => new(_pluginStore, _failed, _arm, _adm, _l);
}

public readonly record struct ValidationProcessResult(int Artifacts, int Resources);
