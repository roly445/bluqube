using Microsoft.Extensions.Logging;

namespace BluQube.Tests.TestHelpers.Fakes;

public class FakeLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel LogLevel, string Message)> _logMessages = new();

    public IReadOnlyList<(LogLevel LogLevel, string Message)> LogMessages => this._logMessages.AsReadOnly();

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        this._logMessages.Add((logLevel, formatter(state, exception)));
    }
}