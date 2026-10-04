using Microsoft.Extensions.Logging;

namespace BlazorApp.RMTWebsite.Tests.Fakes;

// Fake ILogger for testing records log entries for inspection
public class FakeLogger<T> : ILogger<T>
{
    // List of each recorded log message, in order
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Formatter builds the final text from the template and its values
        var message = formatter(state, exception);
        // Add log level and message to entries
        Entries.Add((logLevel, message));
    }
}
