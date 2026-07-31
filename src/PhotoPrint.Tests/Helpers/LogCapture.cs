using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PhotoPrint.Tests.Helpers;

public sealed record LogRecord(LogLevel Level, string Message);

/// <summary>
/// Collects rendered log lines so a test can assert on what was logged, and how often.
/// Use <see cref="LoggerFor{T}"/> to inject into a component directly, or
/// <see cref="LogCaptureProvider"/> to attach to a real <c>ILoggerFactory</c>.
/// </summary>
public sealed class LogCapture
{
    private readonly ConcurrentQueue<LogRecord> _records = new();

    public IReadOnlyList<LogRecord> Records => [.. _records];

    public int CountStartingWith(string prefix, LogLevel level) =>
        _records.Count(r => r.Level == level && r.Message.StartsWith(prefix, StringComparison.Ordinal));

    internal void Add(LogRecord record) => _records.Enqueue(record);

    public ILogger<T> LoggerFor<T>() => new CaptureLogger<T>(this);
}

public sealed class LogCaptureProvider : ILoggerProvider
{
    private readonly LogCapture _capture;

    public LogCaptureProvider(LogCapture capture) => _capture = capture;

    public ILogger CreateLogger(string categoryName) => new CaptureLogger<object>(_capture);

    public void Dispose() { }
}

internal sealed class CaptureLogger<T> : ILogger<T>
{
    private readonly LogCapture _capture;

    public CaptureLogger(LogCapture capture) => _capture = capture;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _capture.Add(new LogRecord(logLevel, formatter(state, exception)));
}
