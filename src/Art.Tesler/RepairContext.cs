using Art.Common;
using Art.Common.Proxies;

namespace Art.Tesler;

public class RepairContext<TPluginStore> where TPluginStore : IRegistryStore
{
    private readonly TPluginStore _pluginStore;
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed;
    private readonly IArtifactRegistrationManager _arm;
    private readonly ArtifactDataManager _adm;
    private readonly IToolLogHandler _l;

    public RepairContext(TPluginStore pluginStore, IReadOnlyDictionary<ArtifactKey, List<ArtifactResourceInfo>> failed, IArtifactRegistrationManager arm, ArtifactDataManager adm, IToolLogHandler l)
    {
        _pluginStore = pluginStore;
        _failed = new Dictionary<ArtifactKey, List<ArtifactResourceInfo>>(failed);
        _arm = arm;
        _adm = adm;
        _l = l;
    }

    public async Task<bool> RepairAsync(List<ArtifactToolProfile> profiles, bool detailed, string? hashAlgorithm)
    {
        // TODO context should accept ChecksumSource
        foreach (ArtifactToolProfile profile in profiles)
        {
            ArtifactToolProfile artifactToolProfile = profile;
            if (artifactToolProfile.Group == null) throw new IOException("Group not specified in profile");
            var context = _pluginStore.LoadRegistry(ArtifactToolProfileUtil.GetID(profile.Tool)); // InvalidOperationException
            if (!context.TryLoad(artifactToolProfile.GetID(), out IArtifactTool? t))
                throw new ArtifactToolNotFoundException(artifactToolProfile.Tool);
            ArtifactToolConfig config = new(_arm, _adm);
            using IArtifactTool tool = t;
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
                        await foreach (IArtifactData data in (new ArtifactToolListProxy(tool, ArtifactToolListOptions.Default, _l).ListAsync()))
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

    private async Task Fixup(IArtifactTool tool, ArtifactKey key, ICollection<ArtifactResourceInfo> list, IArtifactData data, string? hashAlgorithm)
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
