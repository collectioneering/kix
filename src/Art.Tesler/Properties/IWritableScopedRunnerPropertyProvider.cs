namespace Art.Tesler.Properties;

public interface IWritableScopedRunnerPropertyProvider : IScopedRunnerPropertyProvider
{
    bool TrySetProperty(ConfigProperty configProperty);

    bool TryUnsetProperty(ConfigPropertyIdentifier configPropertyIdentifier);
}
