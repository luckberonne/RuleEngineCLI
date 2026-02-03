using FluentAssertions;
using Moq;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Application.UseCases;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using Xunit;

namespace RuleEngineCLI.Application.Tests.UseCases;

/// <summary>
/// Tests del caso de uso principal con mocks.
/// Demuestra testing con Dependency Injection y mocking.
/// </summary>
public class EvaluateRulesUseCaseTests
{
    private readonly Mock<IRuleRepository> _mockRepository;
    private readonly Mock<IExpressionEvaluator> _mockEvaluator;
    private readonly Mock<ILogger> _mockLogger;
    private readonly EvaluateRulesUseCase _useCase;

    public EvaluateRulesUseCaseTests()
    {
        _mockRepository = new Mock<IRuleRepository>();
        _mockEvaluator = new Mock<IExpressionEvaluator>();
        _mockLogger = new Mock<ILogger>();

        _useCase = new EvaluateRulesUseCase(
            _mockRepository.Object,
            _mockEvaluator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoRules_ShouldReturnPassStatus()
    {
        // Arrange
        var input = new ValidationInputDto();
        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("PASS");
        result.TotalRulesEvaluated.Should().Be(0);
        _mockLogger.Verify(l => l.LogWarning("No rules found to evaluate."), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithPassingRule_ShouldReturnPass()
    {
        // Arrange
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var input = new ValidationInputDto();

        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>()))
            .Returns(true);

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule, input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.Status.Should().Be("PASS");
        result.TotalRulesEvaluated.Should().Be(1);
        result.TotalPassed.Should().Be(1);
        result.TotalFailed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingErrorRule_ShouldReturnFail()
    {
        // Arrange
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var input = new ValidationInputDto();

        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>()))
            .Returns(true);

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule, input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.Status.Should().Be("FAIL");
        result.TotalRulesEvaluated.Should().Be(1);
        result.TotalPassed.Should().Be(0);
        result.TotalFailed.Should().Be(1);
        result.MaxSeverity.Should().Be("ERROR");
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedResults_ShouldCalculateCorrectly()
    {
        // Arrange
        var rule1 = CreateTestRule("RULE_001", Severity.Error);
        var rule2 = CreateTestRule("RULE_002", Severity.Warning);
        var rule3 = CreateTestRule("RULE_003", Severity.Info);
        var input = new ValidationInputDto();

        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule1, rule2, rule3 });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>()))
            .Returns(true);

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule1, input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // Pass

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule2, input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Fail (Warning)

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule3, input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // Pass

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.TotalRulesEvaluated.Should().Be(3);
        result.TotalPassed.Should().Be(2);
        result.TotalFailed.Should().Be(1);
        result.Status.Should().Be("WARNING");
        result.MaxSeverity.Should().Be("WARNING");
    }

    [Fact]
    public async Task ExecuteAsync_WhenEvaluatorCannotHandle_ShouldSkipRule()
    {
        // Arrange
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var input = new ValidationInputDto();

        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>()))
            .Returns(false);

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.TotalRulesEvaluated.Should().Be(0);
        _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEvaluatorThrows_ShouldTreatAsFailure()
    {
        // Arrange
        var rule = CreateTestRule("RULE_001", Severity.Error);
        var input = new ValidationInputDto();

        _mockRepository.Setup(r => r.LoadEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });

        _mockEvaluator.Setup(e => e.CanEvaluate(It.IsAny<Rule>()))
            .Returns(true);

        _mockEvaluator.Setup(e => e.EvaluateAsync(rule, input, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Evaluation error"));

        // Act
        var result = await _useCase.ExecuteAsync(input, onlyEnabledRules: true);

        // Assert
        result.TotalFailed.Should().Be(1);
        result.Results[0].Message.Should().Contain("Evaluation error");
        _mockLogger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _useCase.ExecuteAsync(null!, onlyEnabledRules: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
