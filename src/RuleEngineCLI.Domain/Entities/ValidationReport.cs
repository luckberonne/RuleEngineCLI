using RuleEngineCLI.Domain.ValueObjects;

namespace RuleEngineCLI.Domain.Entities;

/// <summary>
/// Aggregate Root que representa el reporte completo de validación.
/// Encapsula la lógica de negocio para determinar el estado final.
/// </summary>
public sealed class ValidationReport
{
    private readonly List<RuleResult> _results;

    public IReadOnlyList<RuleResult> Results => _results.AsReadOnly();
    public int TotalRulesEvaluated => _results.Count;
    public int TotalPassed => _results.Count(r => r.Passed);
    public int TotalFailed => _results.Count(r => !r.Passed);
    public Severity MaxSeverityFound { get; private set; }
    public ValidationStatus Status { get; private set; }
    public DateTime GeneratedAt { get; }

    private ValidationReport(DateTime generatedAt)
    {
        _results = new List<RuleResult>();
        MaxSeverityFound = Severity.Info;
        Status = ValidationStatus.Pass;
        GeneratedAt = generatedAt;
    }

    public static ValidationReport Create()
    {
        return new ValidationReport(DateTime.UtcNow);
    }

    /// <summary>
    /// Añade un resultado de regla al reporte y recalcula el estado.
    /// </summary>
    public void AddResult(RuleResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        _results.Add(result);
        RecalculateStatus();
    }

    /// <summary>
    /// Añade múltiples resultados de forma eficiente.
    /// </summary>
    public void AddResults(IEnumerable<RuleResult> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));

        _results.AddRange(results);
        RecalculateStatus();
    }

    /// <summary>
    /// Lógica de dominio: determina el estado final basado en severidad.
    /// </summary>
    private void RecalculateStatus()
    {
        if (_results.Count == 0)
        {
            Status = ValidationStatus.Pass;
            MaxSeverityFound = Severity.Info;
            return;
        }

        var failedResults = _results.Where(r => !r.Passed).ToList();

        if (failedResults.Count == 0)
        {
            Status = ValidationStatus.Pass;
            MaxSeverityFound = Severity.Info;
            return;
        }

        // Encontrar la severidad máxima entre las reglas fallidas
        MaxSeverityFound = failedResults.Max(r => r.Severity)!;

        // Determinar el estado basado en la severidad máxima
        Status = MaxSeverityFound.Level switch
        {
            SeverityLevel.Info => ValidationStatus.Pass,
            SeverityLevel.Warning => ValidationStatus.Warning,
            SeverityLevel.Error => ValidationStatus.Fail,
            _ => ValidationStatus.Fail
        };
    }

    /// <summary>
    /// Obtiene todos los resultados fallidos agrupados por severidad.
    /// </summary>
    public Dictionary<Severity, List<RuleResult>> GetFailedResultsBySeverity()
    {
        return _results
            .Where(r => !r.Passed)
            .GroupBy(r => r.Severity)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public override string ToString() =>
        $"ValidationReport: {Status} | Total: {TotalRulesEvaluated}, Passed: {TotalPassed}, Failed: {TotalFailed}, Max Severity: {MaxSeverityFound}";
}

public enum ValidationStatus
{
    Pass,
    Warning,
    Fail
}
