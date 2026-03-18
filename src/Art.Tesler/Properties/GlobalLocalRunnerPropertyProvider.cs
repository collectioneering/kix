
using System.Text.Json;

namespace Art.Tesler.Properties;

public class GlobalLocalRunnerPropertyProvider : IWritableScopedRunnerPropertyProvider
{
    private readonly IWritableRunnerPropertyProvider _globalProvider;
    private readonly IWritableRunnerPropertyProvider _localProvider;

    public GlobalLocalRunnerPropertyProvider(
        IWritableRunnerPropertyProvider globalProvider,
        IWritableRunnerPropertyProvider localProvider)
    {
        _globalProvider = globalProvider;
        _localProvider = localProvider;
    }

    public IEnumerable<KeyValuePair<string, JsonElement>> GetProperties()
    {
        return _globalProvider.GetProperties().Concat(_localProvider.GetProperties());
    }

    public IEnumerable<ConfigProperty> GetProperties(ConfigScopeFlags configScopeFlags)
    {
        if ((configScopeFlags & ConfigScopeFlags.Global) != 0)
        {
            foreach (var pair in _globalProvider.GetProperties())
            {
                yield return new ConfigProperty(ConfigScope.Global, pair.Key, pair.Value);
            }
        }
        if ((configScopeFlags & ConfigScopeFlags.Local) != 0)
        {
            foreach (var pair in _localProvider.GetProperties())
            {
                yield return new ConfigProperty(ConfigScope.Local, pair.Key, pair.Value);
            }
        }
    }

    public bool TryGetProperty(string key, ConfigScopeFlags configScopeFlags, out ConfigProperty configProperty)
    {
        if ((configScopeFlags & ConfigScopeFlags.Local) != 0)
        {
            if (_localProvider.TryGetProperty(key, out var subValue))
            {
                configProperty = new ConfigProperty(ConfigScope.Local, key, subValue);
                return true;
            }
        }
        if ((configScopeFlags & ConfigScopeFlags.Global) != 0)
        {
            if (_globalProvider.TryGetProperty(key, out var subValue))
            {
                configProperty = new ConfigProperty(ConfigScope.Global, key, subValue);
                return true;
            }
        }
        configProperty = default;
        return false;
    }

    public bool TryGetProperty(string key, out JsonElement value)
    {
        if (TryGetProperty(key, ConfigScopeFlags.All, out ConfigProperty configProperty))
        {
            value = configProperty.Value;
            return true;
        }
        value = default;
        return false;
    }

    public bool TrySetProperty(ConfigProperty configProperty)
    {
        return configProperty.ConfigScope switch
        {
            ConfigScope.Local => _localProvider.TrySetProperty(configProperty.Key, configProperty.Value),
            ConfigScope.Global => _globalProvider.TrySetProperty(configProperty.Key, configProperty.Value),
            ConfigScope.Profile => false,
            _ => false,
        };
    }

    public bool TryUnsetProperty(ConfigPropertyIdentifier configPropertyIdentifier)
    {
        return configPropertyIdentifier.ConfigScope switch
        {
            ConfigScope.Local => _localProvider.TryUnsetProperty(configPropertyIdentifier.Key),
            ConfigScope.Global => _globalProvider.TryUnsetProperty(configPropertyIdentifier.Key),
            ConfigScope.Profile => false,
            _ => false,
        };
    }
}