namespace Art.Tesler;

public interface IToolLogHandlerProvider : IOutputControl
{
    internal IToolLogHandler GetStreamToolLogHandler(LogPreferences logPreferences);

    internal IToolLogHandler GetDefaultToolLogHandler(LogPreferences logPreferences);

    internal Stream GetOutStream();
}
