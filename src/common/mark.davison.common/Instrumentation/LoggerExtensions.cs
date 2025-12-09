namespace mark.davison.common.Instrumentation;

public static class LoggerExtensions
{

    class LoggingDisposable : IDisposable
    {
        private bool disposedValue;

        private readonly ILogger logger;
        private readonly LogLevel logLevel;
        private readonly string context;
        private readonly EventId end;
        private readonly Stopwatch stopWatch;
        public LoggingDisposable(ILogger logger, LogLevel logLevel, string context, EventId start, EventId end)
        {
            this.logger = logger;
            this.logLevel = logLevel;
            this.context = context;
            this.end = end;

            this.logger.Log(logLevel, end, $"{context} - Beginning");
            stopWatch = Stopwatch.StartNew();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    logger.Log(logLevel, end, $"{context} - Completed - {stopWatch.ElapsedMilliseconds}ms");
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static IDisposable ProfileOperation(this ILogger logger, LogLevel logLevel = LogLevel.Trace, [CallerMemberName] string context = "")
    {
        return new LoggingDisposable(
            logger,
            logLevel,
            context,
            LoggerEvents.MethodBegin,
            LoggerEvents.MethodComplete
        );
    }
}