using System;

namespace RuleEngineCLI.Application.Configuration;

/// <summary>
/// Opciones de configuración para el motor de reglas.
/// </summary>
public class RuleEngineOptions
{
    /// <summary>
    /// Sección de configuración en appsettings.json
    /// </summary>
    public const string SectionName = "RuleEngine";

    /// <summary>
    /// Ruta al archivo de reglas JSON.
    /// </summary>
    public string RulesFilePath { get; set; } = "rules.json";

    /// <summary>
    /// Validar esquema JSON antes de cargar reglas.
    /// </summary>
    public bool ValidateSchema { get; set; } = true;

    /// <summary>
    /// Configuración de caché.
    /// </summary>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Configuración de logging.
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Configuración de evaluación.
    /// </summary>
    public EvaluationOptions Evaluation { get; set; } = new();
}

/// <summary>
/// Opciones de caché.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Habilitar caché de reglas.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tiempo de vida del caché en minutos.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Tamaño máximo del caché (en número de entradas).
    /// </summary>
    public int? MaxSize { get; set; }
}

/// <summary>
/// Opciones de logging.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Nivel mínimo de logging (Debug, Information, Warning, Error).
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Incluir timestamps en los logs.
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Incluir detalles de excepciones.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = true;

    /// <summary>
    /// Formato de logging (Console, Structured, Json).
    /// </summary>
    public string Format { get; set; } = "Console";
}

/// <summary>
/// Opciones de evaluación.
/// </summary>
public class EvaluationOptions
{
    /// <summary>
    /// Evaluador a usar (Comparison, NCalc).
    /// </summary>
    public string EvaluatorType { get; set; } = "Comparison";

    /// <summary>
    /// Continuar evaluación si una regla falla.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Timeout máximo para evaluación (en segundos).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Habilitar métricas de performance.
    /// </summary>
    public bool EnableMetrics { get; set; } = false;
}
