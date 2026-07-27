using FluentAssertions;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Monitoring;
using Xunit;

namespace RuleEngineCLI.Infrastructure.Tests.Monitoring;

public class InstrumentedRuleEngineTests
{
    private sealed class FakeRuleEngine : IRuleEngine
    {
        public int EvaluateAsyncCalls { get; private set; }
        public int EvaluateEnabledRulesAsyncCalls { get; private set; }

        public Task<ValidationReportDto> EvaluateAsync(ValidationInputDto input, CancellationToken cancellationToken = default)
        {
            EvaluateAsyncCalls++;
            return Task.FromResult(BuildReport());
        }

        public Task<ValidationReportDto> EvaluateEnabledRulesAsync(ValidationInputDto input, CancellationToken cancellationToken = default)
        {
            EvaluateEnabledRulesAsyncCalls++;
            return Task.FromResult(BuildReport());
        }

        private static ValidationReportDto BuildReport()
        {
            var report = ValidationReport.Create();
            var rule = Rule.Create(
                RuleId.Create("RULE_001"),
                "Test rule",
                Expression.Create("age > 18"),
                Severity.Error,
                "Test error");
            report.AddResult(RuleResult.Success(rule));
            return ValidationReportDto.FromDomain(report);
        }
    }

    [Fact]
    public async Task EvaluateAsync_DelegatesToInnerEngine_AndReturnsItsResult()
    {
        var inner = new FakeRuleEngine();
        var instrumented = new InstrumentedRuleEngine(inner);

        var report = await instrumented.EvaluateAsync(new ValidationInputDto());

        inner.EvaluateAsyncCalls.Should().Be(1);
        report.TotalRulesEvaluated.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateEnabledRulesAsync_DelegatesToInnerEngine()
    {
        var inner = new FakeRuleEngine();
        var instrumented = new InstrumentedRuleEngine(inner);

        await instrumented.EvaluateEnabledRulesAsync(new ValidationInputDto());

        inner.EvaluateEnabledRulesAsyncCalls.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullInnerEngine_Throws()
    {
        Action act = () => new InstrumentedRuleEngine(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var instrumented = new InstrumentedRuleEngine(new FakeRuleEngine());

        Action act = () => instrumented.Dispose();

        act.Should().NotThrow();
    }
}
