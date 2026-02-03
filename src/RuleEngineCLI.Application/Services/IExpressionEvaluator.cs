using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Domain.Entities;

namespace RuleEngineCLI.Application.Services;

/// <summary>
/// Interfaz para evaluadores de expresiones.
/// Aplica SOLID: Strategy Pattern - permite diferentes implementaciones de evaluación.
/// OCP - abierto a nuevas estrategias de evaluación.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evalúa una expresión contra los datos de entrada.
    /// </summary>
    /// <returns>True si la expresión se cumple, False si falla.</returns>
    Task<bool> EvaluateAsync(
        Rule rule,
        ValidationInputDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el evaluador puede manejar la expresión dada.
    /// </summary>
    bool CanEvaluate(Rule rule);
}
