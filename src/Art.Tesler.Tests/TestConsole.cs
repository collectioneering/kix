namespace Art.Tesler.Tests;

public class TestConsole
{
    public TestConsole(TextWriter outWriter, TextWriter errorWriter)
    {
        Out = outWriter;
        Error = errorWriter;
    }

    public TextWriter Error { get; }

    public TextWriter Out { get; }

}
