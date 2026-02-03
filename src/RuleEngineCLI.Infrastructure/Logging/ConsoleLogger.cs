using RuleEngineCLI.Application.Services;

namespace RuleEngineCLI.Infrastructure.Logging;

/// <summary>
/// Implementación simple de logger para consola.
/// En producción considerar usar Serilog, NLog o Microsoft.Extensions.Logging.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
    private readonly LogLevel _minLevel;
    private readonly bool _includeTimestamp;

    public ConsoleLogger(LogLevel minLevel = LogLevel.Information, bool includeTimestamp = true)
    {
        _minLevel = minLevel;
        _includeTimestamp = includeTimestamp;
    }

    public void LogInformation(string message)
    {
        if (_minLevel <= LogLevel.Information)
            WriteLog("INFO", message, ConsoleColor.White);
    }

    public void LogWarning(string message)
    {
        if (_minLevel <= LogLevel.Warning)
            WriteLog("WARN", message, ConsoleColor.Yellow);
    }

    public void LogError(string message)
    {
        if (_minLevel <= LogLevel.Error)
            WriteLog("ERROR", message, ConsoleColor.Red);
    }

    public void LogError(string message, Exception exception)
    {
        if (_minLevel <= LogLevel.Error)
        {
            WriteLog("ERROR", message, ConsoleColor.Red);
            WriteLog("ERROR", $"Exception: {exception.GetType().Name} - {exception.Message}", ConsoleColor.Red);
            
            if (_minLevel == LogLevel.Debug && exception.StackTrace != null)
                WriteLog("ERROR", exception.StackTrace, ConsoleColor.DarkRed);
        }
    }

    public void LogDebug(string message)
    {
        if (_minLevel <= LogLevel.Debug)
            WriteLog("DEBUG", message, ConsoleColor.Gray);
    }

    private void WriteLog(string level, string message, ConsoleColor color)
    {
        var timestamp = _includeTimestamp ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " : "";
        var prefix = $"{timestamp}[{level}] ";

        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{prefix}{message}");
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}

public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}
