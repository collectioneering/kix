namespace Art.Tesler.Properties;

public interface IScopedToolPropertyProvider : IToolPropertyProvider
{
    IEnumerable<ConfigProperty> GetProperties(ArtifactToolID artifactToolId, ConfigScopeFlags configScopeFlags);

    bool TryGetProperty(ArtifactToolID artifactToolId, string key, ConfigScopeFlags configScopeFlags, out ConfigProperty configProperty);
}