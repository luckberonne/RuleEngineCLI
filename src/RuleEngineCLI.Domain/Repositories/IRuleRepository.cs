using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;

namespace RuleEngineCLI.Domain.Repositories;

/// <summary>
/// Repository interface para acceso a reglas.
/// Aplica SOLID: DIP - el dominio define la abstracción, infraestructura la implementa.
/// ISP - interfaz segregada con métodos específicos.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Carga todas las reglas desde la fuente de datos configurada.
    /// </summary>
    Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga una regla específica por su ID.
    /// </summary>
    Task<Rule?> LoadRuleByIdAsync(RuleId ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga todas las reglas habilitadas.
    /// </summary>
    Task<IEnumerable<Rule>> LoadEnabledRulesAsync(CancellationToken cancellationToken = default);
}
