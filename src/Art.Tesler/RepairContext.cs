using Art.Common;
using Art.Common.Proxies;

namespace Art.Tesler;

public class RepairContext : ToolControlContext
{
    private readonly Dictionary<ArtifactKey, List<ArtifactResourceInfo>> _failed;
    private readonly IArtifactRegistrationManager _arm;
    private readonly IArtifactDataManager _adm;
    private readonly IToolLogHandler _l;

    public RepairContext(
        IArtifactToolRegistryStore pluginStore,
        IReadOnlyDictionary<ArtifactKey, List<ArtifactResourceInfo>> failed,
        IArtifactRegistrationManager arm,
        IArtifactDataManager adm,
        IToolLogHandler l)
        : base(pluginStore)
    {
        _failed = new Dictionary<ArtifactKey, List<ArtifactResourceInfo>>(failed);
        _arm = arm;
        _adm = adm;
        _l = l;
    }

    public async Task<bool> RepairAsync(
        List<ArtifactToolProfile> profiles,
        bool detailed,
        ChecksumSource? checksumSource,
        IExtensionsContext extensionsContext,
        TimeProvider timeProvider,
        bool getArtifactRetrievalTimestamps,
        bool getResourceRetrievalTimestamps,
        bool debugMode,
        IOutputControl console,
        CancellationToken cancellationToken)
    {
        foreach (ArtifactToolProfile originalProfile in profiles)
        {
            using var tool = LoadTool(originalProfile);
            var actualProfile = originalProfile.WithCoreTool(tool);
            string toolName = actualProfile.Tool;
            string group = actualProfile.GetGroupOrFallback(tool.GroupFallback);
            if (!_failed.Keys.Any(v => v.Tool == toolName && v.Group == group))
            {
                continue;
            }
            ArtifactToolConfig config = new(_arm, _adm, extensionsContext, timeProvider, getArtifactRetrievalTimestamps, getResourceRetrievalTimestamps);
            await tool.InitializeAsync(config, actualProfile, cancellationToken).ConfigureAwait(false);
            tool.DebugMode = debugMode;
            switch (tool)
            {
                // ReSharper disable SuspiciousTypeConversion.Global
                case IArtifactFindTool:
                    {
                        var findProxy = new ArtifactToolFindProxy(tool, _l);
                        foreach ((ArtifactKey key, List<ArtifactResourceInfo> list) in _failed.Where(v => v.Key.Tool == actualProfile.Tool && v.Key.Group == group).ToList())
                        {
                            if (await findProxy.FindAsync(key.Id, cancellationToken).ConfigureAwait(false) is { } data)
                            {
                                await Fixup(tool, key, list, data, checksumSource, timeProvider, getResourceRetrievalTimestamps, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                _l.Log($"Failed to obtain artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                            }
                        }
                        break;
                    }
                case IArtifactListTool:
                    {
                        var listProxy = new ArtifactToolListProxy(tool, ArtifactToolListOptions.Default, _l);
                        await foreach (IArtifactData data in listProxy.ListAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (_failed.TryGetValue(data.Info.Key, out List<ArtifactResourceInfo>? list))
                            {
                                await Fixup(tool, data.Info.Key, list, data, checksumSource, timeProvider, getResourceRetrievalTimestamps, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        break;
                    }
                // ReSharper restore SuspiciousTypeConversion.Global
            }
        }
        if (_failed.Count != 0)
        {
            _l.Log($"Failed to reacquire {_failed.Sum(v => v.Value.Count)} resources.", null, LogLevel.Error);
            foreach (ArtifactResourceInfo value in _failed.Values.SelectMany(v => v)) Common.Display(value, detailed, console);
            return false;
        }
        _l.Log("Successfully reacquired all resources.", null, LogLevel.Information);
        return true;
    }

    private async Task Fixup(
        IArtifactTool tool,
        ArtifactKey key,
        ICollection<ArtifactResourceInfo> list,
        IArtifactData data,
        ChecksumSource? checksumSource,
        TimeProvider timeProvider,
        bool getResourceRetrievalTimestamps,
        CancellationToken cancellationToken)
    {
        foreach (ArtifactResourceInfo resource in list.ToList())
        {
            if (!data.TryGetValue(resource.Key, out ArtifactResourceInfo? resourceActual))
            {
                _l.Log($"Failed to obtain resource {resource.GetInfoPathString()} for artifact {key.Tool}/{key.Group}:{key.Id}", null, LogLevel.Error);
                continue;
            }
            await tool.DumpResourceAsync(
                resourceActual,
                ResourceUpdateMode.Hard,
                _l,
                checksumSource,
                timeProvider,
                getResourceRetrievalTimestamps,
                isConcurrent: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            list.Remove(resource);
        }
        if (list.Count == 0) _failed.Remove(key);
    }
}
