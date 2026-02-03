namespace RuleEngineCLI.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula una expresión lógica de validación.
/// Ejemplo: "startDate < endDate" o "age >= 18"
/// </summary>
public sealed class Expression : IEquatable<Expression>
{
    public string Value { get; }

    private Expression(string value)
    {
        Value = value;
    }

    public static Expression Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Expression cannot be null or empty.", nameof(value));

        if (value.Length > 500)
            throw new ArgumentException("Expression cannot exceed 500 characters.", nameof(value));

        // Validación básica: debe contener al menos un operador de comparación o avanzado
        var hasOperator = value.Contains("==") || value.Contains("!=") || 
                         value.Contains(">=") || value.Contains("<=") ||
                         value.Contains(">") || value.Contains("<") ||
                         value.Contains("&&") || value.Contains("||") ||
                         // Operadores avanzados (Fase 3)
                         value.Contains("RegEx", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("Regex", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("Matches", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("In ", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("NotIn", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("Between", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("IsNull", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("IsNotNull", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("StartsWith", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("EndsWith", StringComparison.OrdinalIgnoreCase) ||
                         value.Contains("Contains", StringComparison.OrdinalIgnoreCase);

        if (!hasOperator)
            throw new ArgumentException("Expression must contain at least one comparison, logical, or advanced operator.", nameof(value));

        return new Expression(value.Trim());
    }

    public bool Equals(Expression? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as Expression);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(Expression? left, Expression? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Expression? left, Expression? right) => !(left == right);
}
