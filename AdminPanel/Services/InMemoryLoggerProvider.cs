using System.Collections.Concurrent;

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _logs;

    public InMemoryLoggerProvider(ConcurrentQueue<string> logs)
    {
        _logs = logs;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(_logs, categoryName);
    }

    public void Dispose() { }

    private sealed class InMemoryLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _logs;
        private readonly string _category;

        public InMemoryLogger(
            ConcurrentQueue<string> logs,
            string category)
        {
            _logs = logs;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            var log =
                $"[{DateTime.UtcNow:HH:mm:ss}] " +
                $"[{logLevel}] " +
                $"[{_category}] " +
                message;

            if (exception != null)
                log += Environment.NewLine + exception;

            _logs.Enqueue(log);

            while (_logs.Count > 500)
                _logs.TryDequeue(out _);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}