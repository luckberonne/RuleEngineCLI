using FluentAssertions;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Evaluation;
using Xunit;

namespace RuleEngineCLI.Infrastructure.Tests.Evaluation;

public class ComparisonExpressionEvaluatorTests
{
    private readonly ComparisonExpressionEvaluator _evaluator;

    public ComparisonExpressionEvaluatorTests()
    {
        _evaluator = new ComparisonExpressionEvaluator();
    }

    [Theory]
    [InlineData("age >= 18", true)]
    [InlineData("balance > 0", true)]
    [InlineData("name == \"John\"", true)]
    [InlineData("status != null", true)]
    public void CanEvaluate_WithValidExpression_ShouldReturnTrue(string expression, bool expected)
    {
        // Arrange
        var rule = CreateRule(expression);

        // Act
        var result = _evaluator.CanEvaluate(rule);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task EvaluateAsync_NumericGreaterThan_Pass()
    {
        // Arrange
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 25);

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NumericGreaterThan_Fail()
    {
        // Arrange
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 15);

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_NumericGreaterOrEqual_EdgeCase()
    {
        // Arrange
        var rule = CreateRule("age >= 18");
        var input = CreateInput("age", 18);

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NumericLessThan_Pass()
    {
        // Arrange
        var rule = CreateRule("price < 100");
        var input = CreateInput("price", 50);

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_StringEquals_Pass()
    {
        // Arrange
        var rule = CreateRule("status == \"active\"");
        var input = CreateInput("status", "active");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_StringEquals_CaseInsensitive()
    {
        // Arrange
        var rule = CreateRule("status == \"ACTIVE\"");
        var input = CreateInput("status", "active");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_StringNotEquals_Pass()
    {
        // Arrange
        var rule = CreateRule("role != \"admin\"");
        var input = CreateInput("role", "user");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NullComparison_Pass()
    {
        // Arrange
        var rule = CreateRule("value == null");
        var input = CreateInput("value", null);

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotNullComparison_Pass()
    {
        // Arrange
        var rule = CreateRule("username != null");
        var input = CreateInput("username", "john");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalAnd_BothTrue()
    {
        // Arrange
        var rule = CreateRule("age >= 18 && balance > 0");
        var input = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "age", 25 },
            { "balance", 100 }
        });

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalAnd_OneFalse()
    {
        // Arrange
        var rule = CreateRule("age >= 18 && balance > 0");
        var input = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "age", 25 },
            { "balance", -10 }
        });

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalOr_OneTrue()
    {
        // Arrange
        var rule = CreateRule("role == \"admin\" || role == \"superadmin\"");
        var input = CreateInput("role", "admin");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalOr_BothFalse()
    {
        // Arrange
        var rule = CreateRule("role == \"admin\" || role == \"superadmin\"");
        var input = CreateInput("role", "user");

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_DateComparison_LessThan()
    {
        // Arrange
        var rule = CreateRule("startDate < endDate");
        var input = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "startDate", "2026-01-01" },
            { "endDate", "2026-12-31" }
        });

        // Act
        var result = await _evaluator.EvaluateAsync(rule, input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_MissingProperty_ShouldThrow()
    {
        // Arrange
        var rule = CreateRule("missingProperty > 10");
        var input = CreateInput("otherProperty", 5);

        // Act
        Func<Task> act = async () => await _evaluator.EvaluateAsync(rule, input);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found in input data*");
    }

    private Rule CreateRule(string expression)
    {
        return Rule.Create(
            RuleId.Create("TEST_RULE"),
            "Test rule",
            Expression.Create(expression),
            Severity.Error,
            "Test error");
    }

    private ValidationInputDto CreateInput(string key, object? value)
    {
        return new ValidationInputDto(new Dictionary<string, object?>
        {
            { key, value }
        });
    }
}
