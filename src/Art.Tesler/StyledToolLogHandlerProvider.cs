using Art.Common.Logging;

namespace Art.Tesler;

public class StyledToolLogHandlerProvider : ToolLogHandlerProviderBase
{
    public StyledToolLogHandlerProvider(
        TextWriter outWriter,
        TextWriter warnWriter,
        TextWriter errorWriter,
        Func<Stream> outStreamAccessFunc)
        : base(outWriter, warnWriter, errorWriter, outStreamAccessFunc)
    {
    }

    public override IToolLogHandler GetStreamToolLogHandler(LogPreferences logPreferences)
    {
        return new StyledLogHandler(Out, Warn, Error, logPreferences, true);
    }

    public override IToolLogHandler GetDefaultToolLogHandler(LogPreferences logPreferences)
    {
        return new StyledLogHandler(Out, Warn, Error, logPreferences, false, OperatingSystem.IsMacOS());
    }
}
