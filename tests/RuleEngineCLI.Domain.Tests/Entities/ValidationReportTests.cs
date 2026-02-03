using FluentAssertions;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;
using Xunit;

namespace RuleEngineCLI.Domain.Tests.Entities;

public class ValidationReportTests
{
    [Fact]
    public void Create_ShouldReturnEmptyReport()
    {
        // Act
        var report = ValidationReport.Create();

        // Assert
        report.Should().NotBeNull();
        report.TotalRulesEvaluated.Should().Be(0);
        report.TotalPassed.Should().Be(0);
        report.TotalFailed.Should().Be(0);
        report.Status.Should().Be(ValidationStatus.Pass);
        report.MaxSeverityFound.Should().Be(Severity.Info);
    }

    [Fact]
    public void AddResult_WithPassedResult_ShouldUpdateCounts()
    {
        // Arrange
        var report = ValidationReport.Create();
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var result = RuleResult.Success(rule);

        // Act
        report.AddResult(result);

        // Assert
        report.TotalRulesEvaluated.Should().Be(1);
        report.TotalPassed.Should().Be(1);
        report.TotalFailed.Should().Be(0);
        report.Status.Should().Be(ValidationStatus.Pass);
    }

    [Fact]
    public void AddResult_WithFailedErrorRule_ShouldSetStatusToFail()
    {
        // Arrange
        var report = ValidationReport.Create();
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var result = RuleResult.Failure(rule);

        // Act
        report.AddResult(result);

        // Assert
        report.TotalRulesEvaluated.Should().Be(1);
        report.TotalPassed.Should().Be(0);
        report.TotalFailed.Should().Be(1);
        report.Status.Should().Be(ValidationStatus.Fail);
        report.MaxSeverityFound.Should().Be(Severity.Error);
    }

    [Fact]
    public void AddResult_WithFailedWarningRule_ShouldSetStatusToWarning()
    {
        // Arrange
        var report = ValidationReport.Create();
        var rule = CreateTestRule("RULE_001", Severity.Warning);
        var result = RuleResult.Failure(rule);

        // Act
        report.AddResult(result);

        // Assert
        report.Status.Should().Be(ValidationStatus.Warning);
        report.MaxSeverityFound.Should().Be(Severity.Warning);
    }

    [Fact]
    public void AddResults_WithMixedResults_ShouldCalculateCorrectly()
    {
        // Arrange
        var report = ValidationReport.Create();
        var rule1 = CreateTestRule("RULE_001", Severity.Error);
        var rule2 = CreateTestRule("RULE_002", Severity.Warning);
        var rule3 = CreateTestRule("RULE_003", Severity.Info);

        var results = new[]
        {
            RuleResult.Success(rule1),
            RuleResult.Failure(rule2),
            RuleResult.Success(rule3)
        };

        // Act
        report.AddResults(results);

        // Assert
        report.TotalRulesEvaluated.Should().Be(3);
        report.TotalPassed.Should().Be(2);
        report.TotalFailed.Should().Be(1);
        report.Status.Should().Be(ValidationStatus.Warning);
        report.MaxSeverityFound.Should().Be(Severity.Warning);
    }

    [Fact]
    public void AddResults_WithMultipleFailedRules_ShouldUseMaxSeverity()
    {
        // Arrange
        var report = ValidationReport.Create();
        var rule1 = CreateTestRule("RULE_001", Severity.Warning);
        var rule2 = CreateTestRule("RULE_002", Severity.Error);
        var rule3 = CreateTestRule("RULE_003", Severity.Info);

        var results = new[]
        {
            RuleResult.Failure(rule1),
            RuleResult.Failure(rule2),
            RuleResult.Failure(rule3)
        };

        // Act
        report.AddResults(results);

        // Assert
        report.Status.Should().Be(ValidationStatus.Fail);
        report.MaxSeverityFound.Should().Be(Severity.Error);
    }

    [Fact]
    public void GetFailedResultsBySeverity_ShouldGroupCorrectly()
    {
        // Arrange
        var report = ValidationReport.Create();
        var errorRule1 = CreateTestRule("RULE_001", Severity.Error);
        var errorRule2 = CreateTestRule("RULE_002", Severity.Error);
        var warningRule = CreateTestRule("RULE_003", Severity.Warning);

        report.AddResult(RuleResult.Failure(errorRule1));
        report.AddResult(RuleResult.Failure(errorRule2));
        report.AddResult(RuleResult.Failure(warningRule));

        // Act
        var grouped = report.GetFailedResultsBySeverity();

        // Assert
        grouped.Should().HaveCount(2);
        grouped[Severity.Error].Should().HaveCount(2);
        grouped[Severity.Warning].Should().HaveCount(1);
    }

    private Rule CreateTestRule(string id, Severity severity)
    {
        return Rule.Create(
            RuleId.Create(id),
            "Test rule description",
            Expression.Create("value > 0"),
            severity,
            "Test error message");
    }
}
