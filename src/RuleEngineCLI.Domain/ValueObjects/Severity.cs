namespace RuleEngineCLI.Domain.ValueObjects;

/// <summary>
/// Value Object que representa la severidad de una regla.
/// Usa enum interno para garantizar valores válidos (type-safe).
/// </summary>
public sealed class Severity : IComparable<Severity>, IEquatable<Severity>
{
    public SeverityLevel Level { get; }

    private Severity(SeverityLevel level)
    {
        Level = level;
    }

    public static Severity Info => new(SeverityLevel.Info);
    public static Severity Warning => new(SeverityLevel.Warning);
    public static Severity Error => new(SeverityLevel.Error);

    public static Severity FromString(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "INFO" => Info,
            "WARN" or "WARNING" => Warning,
            "ERROR" => Error,
            _ => throw new ArgumentException($"Invalid severity value: '{value}'. Valid values: INFO, WARN, ERROR.", nameof(value))
        };
    }

    public int CompareTo(Severity? other)
    {
        if (other is null) return 1;
        return Level.CompareTo(other.Level);
    }

    public bool Equals(Severity? other)
    {
        if (other is null) return false;
        return Level == other.Level;
    }

    public override bool Equals(object? obj) => Equals(obj as Severity);

    public override int GetHashCode() => Level.GetHashCode();

    public override string ToString() => Level.ToString().ToUpperInvariant();

    public static bool operator ==(Severity? left, Severity? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Severity? left, Severity? right) => !(left == right);

    public static bool operator >(Severity left, Severity right) => left.CompareTo(right) > 0;
    public static bool operator <(Severity left, Severity right) => left.CompareTo(right) < 0;
    public static bool operator >=(Severity left, Severity right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Severity left, Severity right) => left.CompareTo(right) <= 0;
}

public enum SeverityLevel
{
    Info = 0,
    Warning = 1,
    Error = 2
}
