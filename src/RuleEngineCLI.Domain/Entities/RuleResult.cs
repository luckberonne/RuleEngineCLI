using RuleEngineCLI.Domain.ValueObjects;

namespace RuleEngineCLI.Domain.Entities;

/// <summary>
/// Representa el resultado de la evaluación de una regla individual.
/// Value Object con comportamiento de resultado.
/// </summary>
public sealed class RuleResult
{
    public RuleId RuleId { get; }
    public string RuleDescription { get; }
    public bool Passed { get; }
    public Severity Severity { get; }
    public string Message { get; }
    public DateTime EvaluatedAt { get; }

    private RuleResult(
        RuleId ruleId,
        string ruleDescription,
        bool passed,
        Severity severity,
        string message,
        DateTime evaluatedAt)
    {
        RuleId = ruleId ?? throw new ArgumentNullException(nameof(ruleId));
        RuleDescription = ruleDescription ?? throw new ArgumentNullException(nameof(ruleDescription));
        Passed = passed;
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        EvaluatedAt = evaluatedAt;
    }

    public static RuleResult Success(Rule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        
        return new RuleResult(
            rule.Id,
            rule.Description,
            passed: true,
            rule.Severity,
            "Rule passed successfully.",
            DateTime.UtcNow);
    }

    public static RuleResult Failure(Rule rule, string? additionalContext = null)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var message = string.IsNullOrWhiteSpace(additionalContext)
            ? rule.ErrorMessage
            : $"{rule.ErrorMessage} | Context: {additionalContext}";

        return new RuleResult(
            rule.Id,
            rule.Description,
            passed: false,
            rule.Severity,
            message,
            DateTime.UtcNow);
    }

    public override string ToString() => 
        $"[{(Passed ? "PASS" : "FAIL")}] {RuleId} - {RuleDescription} ({Severity})";
}
