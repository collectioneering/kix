using System.Security.Cryptography;
using Art;
using Art.Crypto;
using Art.Logging;
using Art.Management;
using Art.Resources;

namespace Kix;

public class ValidationContext
{
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed = new();
    private readonly ArtifactRegistrationManager _arm;
    private readonly ArtifactDataManager _adm;
    private readonly bool _debug;
    private readonly bool _addChecksum;
    private readonly IToolLogHandler _l;

    public bool AnyFailed => _failed.Count != 0;

    public int CountResourceFailures() => _failed.Sum(v => v.Value.Count);

    public ValidationContext(ArtifactRegistrationManager arm, ArtifactDataManager adm, bool debug, bool addChecksum, IToolLogHandler l)
    {
        _arm = arm;
        _adm = adm;
        _debug = debug;
        _addChecksum = addChecksum;
        _l = l;
    }

    private void AddFail(ArtifactResourceInfo r)
    {
        if (!_failed.TryGetValue(r.Key.Artifact, out var list)) list = _failed[r.Key.Artifact] = new List<ArtifactResourceInfo>();
        list.Add(r);
    }

    public async Task<ValidationProcessResult> ProcessAsync(List<ArtifactInfo> artifacts)
    {
        int artifactCount = 0, resourceCount = 0;
        foreach (ArtifactInfo inf in artifacts)
        {
            var result = await ProcessAsync(inf);
            artifactCount += result.Artifacts;
            resourceCount += result.Resources;
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public async Task<ValidationProcessResult> ProcessAsync(ArtifactInfo artifact)
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
                if (_addChecksum) AddFail(rInf);
                continue;
            }
            if (!ChecksumSource.TryGetHashAlgorithm(rInf.Checksum.Id, out HashAlgorithm? hashAlgorithm))
            {
                AddFail(rInf);
                continue;
            }
            await using Stream sourceStream = await _adm.OpenInputStreamAsync(rInf.Key);
            await using HashProxyStream hps = new(sourceStream, hashAlgorithm, true);
            await using MemoryStream ms = new();
            await hps.CopyToAsync(ms);
            if (!rInf.Checksum.Value.AsSpan().SequenceEqual(hps.GetHash())) AddFail(rInf);
        }
        return new ValidationProcessResult(1, resourceCount);
    }


    public async Task<ValidationProcessResult> ProcessAsync(IEnumerable<ArtifactToolProfile> profiles)
    {
        int artifactCount = 0, resourceCount = 0;
        foreach (ArtifactToolProfile profile in profiles)
        {
            Common.LoadAssemblyForToolString(profile.Tool); // InvalidOperationException
            ArtifactTool tool;
            if (ArtifactToolLoader.TryLoad(profile, out var toolTmp)) tool = toolTmp;
            else throw new InvalidOperationException($"Unknown tool {profile.Tool}");
            tool.DebugMode = _debug;
            var pp = profile.WithCoreTool(tool);
            _l.Log($"Processing entries for profile {pp.Tool}/{pp.Group}", null, LogLevel.Title);
            var result = await ProcessAsync(await _arm.ListArtifactsAsync(pp.Tool, pp.Group));
            _l.Log($"Processed {result.Artifacts} artifacts and {result.Resources} resources for profile {pp.Tool}/{pp.Group}", null, LogLevel.Information);
            artifactCount += result.Artifacts;
            resourceCount += result.Resources;
        }
        return new ValidationProcessResult(artifactCount, resourceCount);
    }

    public RepairContext CreateRepairContext() => new(_failed, _arm, _adm, _debug, _l);
}

public readonly record struct ValidationProcessResult(int Artifacts, int Resources);
