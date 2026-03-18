namespace Art.Tesler.Properties;

public interface IWritableScopedToolPropertyProvider : IScopedToolPropertyProvider
{
    bool TrySetProperty(ArtifactToolID artifactToolId, ConfigProperty configProperty);

    bool TryUnsetProperty(ArtifactToolID artifactToolId, ConfigPropertyIdentifier configPropertyIdentifier);
}
