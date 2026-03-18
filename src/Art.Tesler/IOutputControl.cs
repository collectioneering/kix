namespace Art.Tesler;

public interface IOutputControl
{
    TextWriter Out { get; }

    TextWriter Warn { get; }

    TextWriter Error { get; }
}
