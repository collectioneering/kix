namespace Art.Tesler;

public class ObjectToolLogHandlerProvider : ToolLogHandlerProviderBase
{
    public Queue<ObjectLog> OutQueue;
    public Queue<ObjectLog> ErrorQueue;

    public ObjectToolLogHandlerProvider(
        TextWriter outWriter,
        TextWriter warnWriter,
        TextWriter errorWriter,
        Func<Stream> outStreamAccessFunc,
        Queue<ObjectLog> outQueue,
        Queue<ObjectLog> errorQueue)
        : base(outWriter, warnWriter, errorWriter, outStreamAccessFunc)
    {
        OutQueue = outQueue;
        ErrorQueue = errorQueue;
    }

    public override ObjectToolLogHandler GetStreamToolLogHandler(LogPreferences logPreferences)
    {
        return new ObjectToolLogHandler(OutQueue, ErrorQueue, true, logPreferences);
    }

    public override ObjectToolLogHandler GetDefaultToolLogHandler(LogPreferences logPreferences)
    {
        return new ObjectToolLogHandler(OutQueue, ErrorQueue, false, logPreferences);
    }
}
