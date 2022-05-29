using Art;
using Art.Logging;
using Art.Management;
using Art.Proxies;
using Art.Resources;

namespace Kix;

public class RepairContext
{
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed;
    private readonly ArtifactRegistrationManager _arm;
    private readonly ArtifactDataManager _adm;
    private readonly bool _debug;
    private readonly IToolLogHandler _l;

    public RepairContext(IReadOnlyDictionary<ArtifactKey, List<ArtifactResourceInfo>> failed, ArtifactRegistrationManager arm, ArtifactDataManager adm, bool debug, IToolLogHandler l)
    {
        _failed = new Dictionary<ArtifactKey, List<ArtifactResourceInfo>>(failed);
        _arm = arm;
        _adm = adm;
        _debug = debug;
        _l = l;
    }

    public async Task<bool> RepairAsync(List<ArtifactToolProfile> profiles, bool detailed, string hashAlgorithm)
    {
        foreach (ArtifactToolProfile profile in profiles)
        {
            ArtifactToolProfile artifactToolProfile = profile;
            if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
            Common.LoadAssemblyForToolString(profile.Tool); // InvalidOperationException
            if (!ArtifactToolLoader.TryLoad(artifactToolProfile, out ArtifactTool? t))
                throw new ArtifactToolNotFoundException(artifactToolProfile.Tool);
            ArtifactToolConfig config = new(_arm, _adm, FailureFlags.None);
            using ArtifactTool tool = t;
            tool.DebugMode = _debug;
            artifactToolProfile = artifactToolProfile.WithCoreTool(t);
            if (!_failed.Keys.Any(v => v.Tool == artifactToolProfile.Tool && v.Group == artifactToolProfile.Group))
                continue;
            await tool.InitializeAsync(config, artifactToolProfile).ConfigureAwait(false);
            switch (tool)
            {
                // ReSharper disable SuspiciousTypeConversion.Global
                case IArtifactToolFind:
                    {
                        var proxy = new ArtifactToolFindProxy(tool);
                        foreach ((ArtifactKey key, List<ArtifactResourceInfo> list) in _failed.Where(v => v.Key.Tool == artifactToolProfile.Tool && v.Key.Group == artifactToolProfile.Group).ToList())
                            if (await proxy.FindAsync(key.Id) is { } data) await Fixup(tool, key, list, data, hashAlgorithm);
                            else _l.Log($"Failed to obtain artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                        break;
                    }
                case IArtifactToolList:
                    {
                        await foreach (ArtifactData data in (new ArtifactToolListProxy(tool, ArtifactToolListOptions.Default, _l).ListAsync()))
                            if (_failed.TryGetValue(data.Info.Key, out List<ArtifactResourceInfo>? list))
                                await Fixup(tool, data.Info.Key, list, data, hashAlgorithm);
                        break;
                    }
                // ReSharper restore SuspiciousTypeConversion.Global
            }
        }
        if (_failed.Count != 0)
        {
            _l.Log($"Failed to reacquire {_failed.Sum(v => v.Value.Count)} resources.", null, LogLevel.Error);
            foreach (ArtifactResourceInfo value in _failed.Values.SelectMany(v => v)) Common.Display(value, detailed);
            return false;
        }
        else
            _l.Log("Successfully reacquired all resources.", null, LogLevel.Information);
        return true;
    }

    private async Task Fixup(ArtifactTool tool, ArtifactKey key, ICollection<ArtifactResourceInfo> list, ArtifactData data, string hashAlgorithm)
    {
        foreach (ArtifactResourceInfo resource in list.ToList())
        {
            if (!data.TryGetValue(resource.Key, out ArtifactResourceInfo? resourceActual))
            {
                _l.Log($"Failed to obtain resource {resource.GetInfoPathString()} for artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                continue;
            }
            await tool.DumpResourceAsync(resourceActual, ResourceUpdateMode.Hard, _l, hashAlgorithm);
            list.Remove(resource);
        }
        if (list.Count == 0) _failed.Remove(key);
    }
}
