namespace RuleEngineCLI.Domain.ValueObjects;

/// <summary>
/// Value Object que representa el identificador único de una regla.
/// Aplica SOLID: SRP - responsabilidad única de identificar reglas de forma inmutable.
/// </summary>
public sealed class RuleId : IEquatable<RuleId>
{
    public string Value { get; }

    private RuleId(string value)
    {
        Value = value;
    }

    public static RuleId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Rule ID cannot be null or empty.", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("Rule ID cannot exceed 100 characters.", nameof(value));

        return new RuleId(value.Trim());
    }

    public bool Equals(RuleId? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as RuleId);

    public override int GetHashCode() => Value.ToUpperInvariant().GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(RuleId? left, RuleId? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RuleId? left, RuleId? right) => !(left == right);
}
