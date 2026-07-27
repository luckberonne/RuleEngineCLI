using FluentAssertions;
using Moq;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Implementation;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using Xunit;

namespace RuleEngineCLI.Application.Tests.Implementation;

public class ParallelRuleEngineTests
{
    private readonly Mock<IRuleRepository> _mockRepository;
    private readonly Mock<IExpressionEvaluator> _mockEvaluator;
    private readonly Mock<ILogger> _mockLogger;

    public ParallelRuleEngineTests()
    {
        _mockRepository = new Mock<IRuleRepository>();
        _mockEvaluator = new Mock<IExpressionEvaluator>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task EvaluateAsync_WithNoRules_ReturnsPassStatus()
    {
        _mockRepository.Setup(r => r.LoadAllRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());

        var engine = CreateEngine();

        var result = await engine.EvaluateAsync(new ValidationInputDto());

        result.Status.Should().Be("PASS");
        result.TotalRulesEvaluated.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateAsync_EvaluatesAllRulesConcurrently_AndAggregatesResults()
    {
        var rules = Enumerable.Range(1, 20)
            .Select(i => CreateTestRule($"RULE_{i:000}", Severity.Error))
            .ToList();

        _mockRepository.Setup(r => r.LoadAllRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>())).Returns(true);
        _mockEvaluator.Setup(e => e.EvaluateAsync(It.IsAny<Rule>(), It.IsAny<ValidationInputDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = CreateEngine();

        var result = await engine.EvaluateAsync(new ValidationInputDto());

        result.TotalRulesEvaluated.Should().Be(20);
        result.TotalPassed.Should().Be(20);
        result.Status.Should().Be("PASS");
    }

    [Fact]
    public async Task EvaluateAsync_WhenEvaluatorThrows_TreatsRuleAsFailedWithoutAbortingOthers()
    {
        var okRule = CreateTestRule("RULE_OK", Severity.Error);
        var badRule = CreateTestRule("RULE_BAD", Severity.Error);

        _mockRepository.Setup(r => r.LoadAllRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { okRule, badRule });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>())).Returns(true);
        _mockEvaluator.Setup(e => e.EvaluateAsync(okRule, It.IsAny<ValidationInputDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockEvaluator.Setup(e => e.EvaluateAsync(badRule, It.IsAny<ValidationInputDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var engine = CreateEngine();

        var result = await engine.EvaluateAsync(new ValidationInputDto());

        result.TotalRulesEvaluated.Should().Be(2);
        result.TotalPassed.Should().Be(1);
        result.TotalFailed.Should().Be(1);
    }

    [Fact]
    public async Task EvaluateEnabledRulesAsync_LoadsOnlyEnabledRules()
    {
        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());

        var engine = CreateEngine();

        await engine.EvaluateEnabledRulesAsync(new ValidationInputDto());

        _mockRepository.Verify(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.LoadAllRulesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_WithNullInput_ThrowsArgumentNullException()
    {
        var engine = CreateEngine();

        Func<Task> act = async () => await engine.EvaluateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private ParallelRuleEngine CreateEngine() =>
        new(_mockRepository.Object, new[] { _mockEvaluator.Object }, _mockLogger.Object);

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
