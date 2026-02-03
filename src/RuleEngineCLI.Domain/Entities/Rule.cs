using RuleEngineCLI.Domain.ValueObjects;

namespace RuleEngineCLI.Domain.Entities;

/// <summary>
/// Entidad que representa una regla de negocio configurable.
/// Entity del dominio con identidad única (RuleId).
/// Aplica SOLID: OCP - abierta a extensión (nuevos tipos de reglas) cerrada a modificación.
/// </summary>
public sealed class Rule
{
    public RuleId Id { get; }
    public string Description { get; }
    public Expression Expression { get; }
    public Severity Severity { get; }
    public string ErrorMessage { get; }
    public bool IsEnabled { get; private set; }

    private Rule(
        RuleId id,
        string description,
        Expression expression,
        Severity severity,
        string errorMessage,
        bool isEnabled = true)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method para crear una regla con validaciones de dominio.
    /// </summary>
    public static Rule Create(
        RuleId id,
        string description,
        Expression expression,
        Severity severity,
        string errorMessage,
        bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));

        if (errorMessage.Length > 1000)
            throw new ArgumentException("Error message cannot exceed 1000 characters.", nameof(errorMessage));

        return new Rule(id, description.Trim(), expression, severity, errorMessage.Trim(), isEnabled);
    }

    /// <summary>
    /// Habilita la regla para evaluación.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Deshabilita la regla (no se evaluará).
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    public override string ToString() => 
        $"[{Id}] {Description} (Severity: {Severity}, Enabled: {IsEnabled})";
}
