namespace Art.Tesler.Properties;

public class SimplePropertyFormatter : PropertyFormatter
{
    public static readonly SimplePropertyFormatter Instance = new();

    public bool PrettyPrint { get; }

    public SimplePropertyFormatter(bool prettyPrint = false)
    {
        PrettyPrint = prettyPrint;
    }

    public override string FormatProperty(ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(configProperty.Key, configProperty.Value, PrettyPrint);
    }

    public override string FormatProperty(ArtifactToolID artifactToolId, ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(configProperty.Key, configProperty.Value, PrettyPrint);
    }

    public override string FormatProperty(int profileIndex, ArtifactToolProfile artifactToolProfile, ArtifactToolID artifactToolId, ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(configProperty.Key, configProperty.Value, PrettyPrint);
    }
}
