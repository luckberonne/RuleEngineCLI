namespace RuleEngineCLI.Application.Services;

/// <summary>
/// Interfaz para servicios de logging.
/// Aplica SOLID: DIP - abstracción definida por Application.
/// ISP - interfaz segregada con métodos específicos de logging.
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogError(string message, Exception exception);
    void LogDebug(string message);
}
