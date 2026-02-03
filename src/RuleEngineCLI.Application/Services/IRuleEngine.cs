using RuleEngineCLI.Application.DTOs;

namespace RuleEngineCLI.Application.Services;

/// <summary>
/// Interfaz que define el contrato del motor de reglas.
/// Aplica SOLID: ISP - interfaz específica para evaluación de reglas.
/// DIP - Application define la abstracción.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evalúa todas las reglas contra los datos de entrada.
    /// </summary>
    Task<ValidationReportDto> EvaluateAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evalúa solo las reglas habilitadas.
    /// </summary>
    Task<ValidationReportDto> EvaluateEnabledRulesAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default);
}
