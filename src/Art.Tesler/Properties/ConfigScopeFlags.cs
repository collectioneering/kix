namespace Art.Tesler.Properties;

[Flags]
public enum ConfigScopeFlags
{
    None = 0,
    Local = 1 << 0,
    Global = 1 << 1,
    Profile = 1 << 2,
    All = ~0
}
