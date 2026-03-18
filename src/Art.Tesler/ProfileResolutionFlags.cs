namespace Art.Tesler;

[Flags]
public enum ProfileResolutionFlags
{
    None = 0,
    Files = 1 << 0,
    KeySelection = 1 << 1,
    Default = Files | KeySelection
}
