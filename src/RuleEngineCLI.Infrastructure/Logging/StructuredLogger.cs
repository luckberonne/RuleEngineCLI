using System;
using RuleEngineCLI.Application.Services;

namespace RuleEngineCLI.Infrastructure.Logging;

/// <summary>
/// Logger estructurado con soporte para diferentes formatos.
/// </summary>
public class StructuredLogger : ILogger
{
    private readonly LogLevel _minimumLevel;
    private readonly bool _includeTimestamp;
    private readonly bool _includeExceptionDetails;
    private readonly LogFormat _format;

    public StructuredLogger(
        LogLevel minimumLevel = LogLevel.Information,
        bool includeTimestamp = true,
        bool includeExceptionDetails = true,
        LogFormat format = LogFormat.Console)
    {
        _minimumLevel = minimumLevel;
        _includeTimestamp = includeTimestamp;
        _includeExceptionDetails = includeExceptionDetails;
        _format = format;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < _minimumLevel)
            return;

        var logEntry = CreateLogEntry(level, message, exception);

        switch (_format)
        {
            case LogFormat.Console:
                LogConsole(logEntry);
                break;
            case LogFormat.Structured:
                LogStructured(logEntry);
                break;
            case LogFormat.Json:
                LogJson(logEntry);
                break;
        }
    }

    public void LogDebug(string message) => Log(LogLevel.Debug, message);
    public void LogInformation(string message) => Log(LogLevel.Information, message);
    public void LogWarning(string message) => Log(LogLevel.Warning, message);
    public void LogError(string message) => Log(LogLevel.Error, message);
    public void LogError(string message, Exception exception) => Log(LogLevel.Error, message, exception);

    private LogEntry CreateLogEntry(LogLevel level, string message, Exception? exception)
    {
        return new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = exception
        };
    }

    private void LogConsole(LogEntry entry)
    {
        var color = GetLevelColor(entry.Level);
        var levelText = GetLevelText(entry.Level);

        Console.ForegroundColor = color;
        if (_includeTimestamp)
        {
            Console.Write($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] ");
        }
        Console.Write($"[{levelText}] ");
        Console.ResetColor();
        Console.WriteLine(entry.Message);

        if (_includeExceptionDetails && entry.Exception != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Exception: {entry.Exception.GetType().Name} - {entry.Exception.Message}");
            if (entry.Exception.StackTrace != null)
            {
                Console.WriteLine(entry.Exception.StackTrace);
            }
            Console.ResetColor();
        }
    }

    private void LogStructured(LogEntry entry)
    {
        var levelText = GetLevelText(entry.Level);
        var timestamp = _includeTimestamp ? $"Timestamp=\"{entry.Timestamp:O}\" " : "";
        var exception = _includeExceptionDetails && entry.Exception != null
            ? $" Exception=\"{entry.Exception.GetType().Name}: {entry.Exception.Message}\""
            : "";

        Console.WriteLine($"{timestamp}Level=\"{levelText}\" Message=\"{entry.Message}\"{exception}");
    }

    private void LogJson(LogEntry entry)
    {
        var timestamp = _includeTimestamp ? $"\"timestamp\":\"{entry.Timestamp:O}\"," : "";
        var exception = _includeExceptionDetails && entry.Exception != null
            ? $",\"exception\":{{\"type\":\"{entry.Exception.GetType().Name}\",\"message\":\"{EscapeJson(entry.Exception.Message)}\"}}"
            : "";

        Console.WriteLine($"{{{timestamp}\"level\":\"{GetLevelText(entry.Level)}\",\"message\":\"{EscapeJson(entry.Message)}\"{exception}}}");
    }

    private ConsoleColor GetLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
    }

    private string GetLevelText(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => "INFO"
        };
    }

    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}

/// <summary>
/// Formato de salida del logger.
/// </summary>
public enum LogFormat
{
    Console,
    Structured,
    Json
}
