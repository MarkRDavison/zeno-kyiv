namespace mark.davison.common.Instrumentation;

[ExcludeFromCodeCoverage]
public static class LoggerEvents
{

    public static readonly EventId MethodBegin = new EventId(1, "Method Begin");
    public static readonly EventId MethodComplete = new EventId(2, "Method Complete");
    public static readonly EventId ValidationFailed = new EventId(3, "Validation Failed");
    public static readonly EventId ValidationPassed = new EventId(4, "Validation Passed");
    public static readonly EventId WebRequestError = new EventId(5, "Web Request Error");

}