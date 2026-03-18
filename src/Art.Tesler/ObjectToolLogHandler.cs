namespace Art.Tesler;

public class ObjectToolLogHandler : IToolLogHandler
{
    public Queue<ObjectLog> OutQueue;
    public Queue<ObjectLog> ErrorQueue;
    public bool AlwaysPrintToErrorStream;

    public LogPreferences LogPreferences { get; set; }

    public ObjectToolLogHandler(Queue<ObjectLog> outQueue, Queue<ObjectLog> errorQueue, bool alwaysPrintToErrorStream, LogPreferences logPreferences)
    {
        OutQueue = outQueue;
        ErrorQueue = errorQueue;
        AlwaysPrintToErrorStream = alwaysPrintToErrorStream;
        LogPreferences = logPreferences;
    }

    public void Log(ObjectLog objectLog)
    {
        SelectQueue(objectLog.Level).Enqueue(objectLog);
    }

    public void Log(string? title, string? body, LogLevel level)
    {
        Log(new ObjectLog(title, body, level));
    }

    public void Log(string tool, string group, string? title, string? body, LogLevel level)
    {
        Log(new ToolObjectLog(title, body, level, tool, group));
    }

    private Queue<ObjectLog> SelectQueue(LogLevel level)
    {
        if (AlwaysPrintToErrorStream)
        {
            return ErrorQueue;
        }
        return level switch
        {
            LogLevel.Information => OutQueue,
            LogLevel.Entry => OutQueue,
            LogLevel.Title => OutQueue,
            LogLevel.Warning => ErrorQueue,
            LogLevel.Error => ErrorQueue,
            _ => ErrorQueue
        };
    }
}
