namespace Art.Tesler;

public class TextWriterOutputControl : IOutputControl
{
    public TextWriter Out { get; }

    public TextWriter Warn { get; }

    public TextWriter Error { get; }

    public TextWriterOutputControl(TextWriter outWriter, TextWriter errorWriter)
    {
        Out = outWriter;
        Warn = errorWriter;
        Error = errorWriter;
    }
}
