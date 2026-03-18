namespace Art.Tesler.Properties;

public class DefaultPropertyFormatter : PropertyFormatter
{
    public static readonly DefaultPropertyFormatter Instance = new();

    public bool PrettyPrint { get; }

    public DefaultPropertyFormatter(bool prettyPrint = false)
    {
        PrettyPrint = prettyPrint;
    }

    public override string FormatProperty(ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(configProperty, PrettyPrint);
    }

    public override string FormatProperty(ArtifactToolID artifactToolId, ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(configProperty, PrettyPrint);
    }

    public override string FormatProperty(int profileIndex, ArtifactToolProfile artifactToolProfile, ArtifactToolID artifactToolId, ConfigProperty configProperty)
    {
        return ConfigPropertyUtility.FormatPropertyForDisplay(profileIndex, artifactToolProfile.Group ?? "<unspecified>", artifactToolId, configProperty, PrettyPrint);
    }
}
