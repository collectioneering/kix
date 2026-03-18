using System.Diagnostics.CodeAnalysis;

namespace Art.Tesler.Profiles;

public class SelectableToolProfileResolver : IProfileResolver
{
    private List<IArtifactToolSelectableRegistry<string>>? _selectableRegistries;
    private readonly IArtifactToolRegistryStore _pluginStore;

    public SelectableToolProfileResolver(IArtifactToolRegistryStore pluginStore)
    {
        _pluginStore = pluginStore;
    }

    public bool TryGetProfiles(string text, [NotNullWhen(true)] out IResolvedProfiles? resolvedProfiles, ProfileResolutionFlags profileResolutionFlags = ProfileResolutionFlags.Default)
    {
        if ((profileResolutionFlags & ProfileResolutionFlags.KeySelection) == 0)
        {
            resolvedProfiles = null;
            return false;
        }
        if (_selectableRegistries == null)
        {
            _selectableRegistries = [];
            foreach (var registry in _pluginStore.LoadAllRegistries())
            {
                if (registry is IArtifactToolSelectableRegistry<string> selectableRegistry)
                {
                    _selectableRegistries.Add(selectableRegistry);
                }
            }
        }
        if (!PurificationUtil.TryIdentify(_selectableRegistries, text, out var profile))
        {
            resolvedProfiles = null;
            return false;
        }
        resolvedProfiles = new DefaultResolvedProfiles([profile]);
        return true;
    }
}
