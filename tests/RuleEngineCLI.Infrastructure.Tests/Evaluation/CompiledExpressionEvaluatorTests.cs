using FluentAssertions;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Evaluation;
using Xunit;

namespace RuleEngineCLI.Infrastructure.Tests.Evaluation;

public class CompiledExpressionEvaluatorTests
{
    private readonly CompiledExpressionEvaluator _evaluator = new();

    [Theory]
    [InlineData("age > 18", true)]
    [InlineData("status == \"active\"", true)]
    public void CanEvaluate_DetectsSupportedOperators(string expression, bool expected)
    {
        var rule = CreateRule(expression);

        _evaluator.CanEvaluate(rule).Should().Be(expected);
    }

    [Fact]
    public async Task EvaluateAsync_NumericComparison_Pass()
    {
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 25);

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NumericComparison_Fail()
    {
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 10);

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalAnd_BothTrue()
    {
        var rule = CreateRule("age >= 18 && balance > 0");
        var input = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "age", 25 },
            { "balance", 100 }
        });

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalOr_OneTrue()
    {
        var rule = CreateRule("age < 18 || age > 65");
        var input = CreateInput("age", 70);

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FieldIsDoubleFromJson_ComparesAgainstIntLiteral()
    {
        // System.Text.Json deserializa números como double; el literal "18" compila a int.
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 25.0);

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ComparingTwoDynamicFields_Works()
    {
        var rule = CreateRule("startDate < endDate");
        var input = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "startDate", "2026-01-01" },
            { "endDate", "2026-12-31" }
        });

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_UnquotedStringLiteral_ComparesAsLiteralNotAsMissingField()
    {
        // "company.com" no está entre comillas y no es un campo del input:
        // debe tratarse como literal de texto, igual que ComparisonExpressionEvaluator.
        var rule = CreateRule("emailDomain == company.com");
        var input = CreateInput("emailDomain", "company.com");

        var result = await _evaluator.EvaluateAsync(rule, input);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_CachesCompiledExpression()
    {
        var rule = CreateRule("age > 18");
        var input = CreateInput("age", 25);

        _evaluator.Evaluate(rule, input);
        _evaluator.Evaluate(rule, input);

        _evaluator.GetCacheSize().Should().Be(1);
    }

    [Fact]
    public void ClearCache_RemovesCompiledDelegates()
    {
        var rule = CreateRule("age > 18");
        _evaluator.Evaluate(rule, CreateInput("age", 25));

        _evaluator.ClearCache();

        _evaluator.GetCacheSize().Should().Be(0);
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

    private ValidationInputDto CreateInput(string key, object value)
    {
        return new ValidationInputDto(new Dictionary<string, object?> { { key, value } });
    }
}
