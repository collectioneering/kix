using System.Text.Json;
using Art.Common;

namespace Art.Tesler.Properties;

public static class TeslerPropertyUtility
{
    public static void ApplyProperties(
        IRunnerPropertyProvider runnerDefaultPropertyProvider,
        IDictionary<string, JsonElement> dictionary)
    {
        foreach (var pair in runnerDefaultPropertyProvider.GetProperties())
        {
            dictionary[pair.Key] = pair.Value;
        }
    }

    public static void ApplyProperties(
        IToolPropertyProvider toolPropertyProvider,
        IDictionary<string, JsonElement> dictionary,
        ArtifactToolID artifactToolId)
    {
        foreach (var pair in toolPropertyProvider.GetProperties(artifactToolId))
        {
            dictionary[pair.Key] = pair.Value;
        }
    }

    public static void ApplyPropertiesDeep(
        IToolPropertyProvider toolPropertyProvider,
        IDictionary<string, JsonElement> dictionary,
        Type type
    )
    {
        if (type.BaseType is { } baseType)
        {
            ApplyPropertiesDeep(toolPropertyProvider, dictionary, baseType);
        }
        ApplyProperties(toolPropertyProvider, dictionary, ArtifactToolIDUtil.CreateToolID(type));
    }

    public static IEnumerable<ConfigProperty> GetPropertiesDeep(
        IScopedToolPropertyProvider toolPropertyProvider,
        Type type,
        ConfigScopeFlags configScopeFlags)
    {
        IEnumerable<ConfigProperty> enumerable = toolPropertyProvider.GetProperties(ArtifactToolIDUtil.CreateCoreToolID(type), configScopeFlags);
        if (type.BaseType is { } baseType)
        {
            return GetPropertiesDeep(toolPropertyProvider, baseType, configScopeFlags).Concat(enumerable);
        }
        return enumerable;
    }

    public static bool TryGetPropertyDeep(IScopedToolPropertyProvider toolPropertyProvider, Type type, string key, ConfigScopeFlags configScopeFlags, out ConfigProperty configProperty)
    {
        while (true)
        {
            if (toolPropertyProvider.TryGetProperty(ArtifactToolIDUtil.CreateCoreToolID(type), key, configScopeFlags, out configProperty))
            {
                return true;
            }
            if (type.BaseType is { } baseType)
            {
                type = baseType;
                continue;
            }
            configProperty = default;
            return false;
        }
    }

    public static void ApplyPropertiesDeep(
        IArtifactToolRegistryStore registryStore,
        IToolPropertyProvider toolPropertyProvider,
        IOutputControl? console,
        Dictionary<string, JsonElement> opts,
        ArtifactToolID artifactToolId)
    {
        if (registryStore.TryLoadRegistry(artifactToolId, out var registry))
        {
            if (registry.TryGetType(artifactToolId, out var type))
            {
                ApplyPropertiesDeep(toolPropertyProvider, opts, type);
            }
            else
            {
                console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found in the registry it should be stored in, configuration will not contain values inherited from base types");
                ApplyProperties(toolPropertyProvider, opts, artifactToolId);
            }
        }
        else
        {
            console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found, configuration will not contain values inherited from base types");
            ApplyProperties(toolPropertyProvider, opts, artifactToolId);
        }
    }

    public static IEnumerable<ConfigProperty> GetPropertiesDeep(
        IArtifactToolRegistryStore registryStore,
        IScopedToolPropertyProvider toolPropertyProvider,
        IOutputControl? console,
        ArtifactToolID artifactToolId,
        ConfigScopeFlags configScopeFlags)
    {
        if (registryStore.TryLoadRegistry(artifactToolId, out var registry))
        {
            if (registry.TryGetType(artifactToolId, out var type))
            {
                return GetPropertiesDeep(toolPropertyProvider, type, configScopeFlags);
            }
            else
            {
                console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found in the registry it should be stored in, configuration will not contain values inherited from base types");
                return toolPropertyProvider.GetProperties(artifactToolId, configScopeFlags);
            }
        }
        else
        {
            console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found, configuration will not contain values inherited from base types");
            return toolPropertyProvider.GetProperties(artifactToolId, configScopeFlags);
        }
    }

    public static bool TryGetPropertyDeep(
        IArtifactToolRegistryStore registryStore,
        IScopedToolPropertyProvider toolPropertyProvider,
        IOutputControl? console,
        ArtifactToolID artifactToolId,
        string key,
        ConfigScopeFlags configScopeFlags,
        out ConfigProperty configProperty)
    {
        if (registryStore.TryLoadRegistry(artifactToolId, out var registry))
        {
            if (registry.TryGetType(artifactToolId, out var type))
            {
                return TryGetPropertyDeep(toolPropertyProvider, type, key, configScopeFlags, out configProperty);
            }
            else
            {
                console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found in the registry it should be stored in, configuration will not contain values inherited from base types");
                return toolPropertyProvider.TryGetProperty(artifactToolId, key, configScopeFlags, out configProperty);
            }
        }
        else
        {
            console?.Warn.WriteLine($"Warning: tool type {artifactToolId.GetToolString()} could not be found, configuration will not contain values inherited from base types");
            return toolPropertyProvider.TryGetProperty(artifactToolId, key, configScopeFlags, out configProperty);
        }
    }

    public static Dictionary<string, JsonElement> GetOptionsMapWithAddedPair(IReadOnlyDictionary<string, JsonElement>? existingOptions, string key, JsonElement value)
    {
        Dictionary<string, JsonElement> map = existingOptions != null ? new(existingOptions) : new();
        map[key] = value;
        return map;
    }

    public static Dictionary<string, JsonElement>? GetOptionsMapWithRemovedKey(IReadOnlyDictionary<string, JsonElement>? existingOptions, string key)
    {
        Dictionary<string, JsonElement>? map;
        if (existingOptions == null)
        {
            map = null;
        }
        else
        {
            if (existingOptions.ContainsKey(key))
            {
                if (existingOptions.Count == 1)
                {
                    map = null;
                }
                else
                {
                    map = new Dictionary<string, JsonElement>(existingOptions);
                    map.Remove(key);
                }
            }
            else
            {
                map = null;
            }
        }
        return map;
    }

    public static Dictionary<string, JsonElement> AddPairToOptionsMap(Dictionary<string, JsonElement>? map, string key, JsonElement value, out bool toCreate)
    {
        if (map == null)
        {
            toCreate = true;
            map = new Dictionary<string, JsonElement>();
        }
        else
        {
            toCreate = false;
        }
        map[key] = value;
        return map;
    }

    public static void RemoveKeyFromOptionsMap(Dictionary<string, JsonElement>? map, string key, out bool toDelete)
    {
        if (map == null)
        {
            toDelete = true;
        }
        else
        {
            if (map.Count == 0 || !map.ContainsKey(key))
            {
                toDelete = true;
            }
            else
            {
                toDelete = map.Count == 1;
                map.Remove(key);
            }
        }
    }
}
