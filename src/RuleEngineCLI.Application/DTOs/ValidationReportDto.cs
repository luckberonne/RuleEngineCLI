using RuleEngineCLI.Domain.Entities;

namespace RuleEngineCLI.Application.DTOs;

/// <summary>
/// DTO para el resultado de validación expuesto a capas externas.
/// </summary>
public sealed class ValidationReportDto
{
    public required int TotalRulesEvaluated { get; init; }
    public required int TotalPassed { get; init; }
    public required int TotalFailed { get; init; }
    public required string MaxSeverity { get; init; }
    public required string Status { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required List<RuleResultDto> Results { get; init; }

    public static ValidationReportDto FromDomain(ValidationReport report)
    {
        return new ValidationReportDto
        {
            TotalRulesEvaluated = report.TotalRulesEvaluated,
            TotalPassed = report.TotalPassed,
            TotalFailed = report.TotalFailed,
            MaxSeverity = report.MaxSeverityFound.ToString(),
            Status = report.Status.ToString().ToUpperInvariant(),
            GeneratedAt = report.GeneratedAt,
            Results = report.Results.Select(RuleResultDto.FromDomain).ToList()
        };
    }
}

public sealed class RuleResultDto
{
    public required string RuleId { get; init; }
    public required string Description { get; init; }
    public required bool Passed { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public required DateTime EvaluatedAt { get; init; }

    public static RuleResultDto FromDomain(RuleResult result)
    {
        return new RuleResultDto
        {
            RuleId = result.RuleId.ToString(),
            Description = result.RuleDescription,
            Passed = result.Passed,
            Severity = result.Severity.ToString(),
            Message = result.Message,
            EvaluatedAt = result.EvaluatedAt
        };
    }
}
