using FluentAssertions;
using RuleEngineCLI.Domain.ValueObjects;
using Xunit;

namespace RuleEngineCLI.Domain.Tests.ValueObjects;

/// <summary>
/// Tests unitarios para RuleId Value Object.
/// Valida: creación, validación, igualdad, inmutabilidad.
/// </summary>
public class RuleIdTests
{
    [Fact]
    public void Create_WithValidId_ShouldReturnRuleId()
    {
        // Arrange
        var id = "RULE_001";

        // Act
        var ruleId = RuleId.Create(id);

        // Assert
        ruleId.Should().NotBeNull();
        ruleId.Value.Should().Be(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldThrowArgumentException(string? invalidId)
    {
        // Act
        Action act = () => RuleId.Create(invalidId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void Create_WithTooLongId_ShouldThrowArgumentException()
    {
        // Arrange
        var longId = new string('A', 101);

        // Act
        Action act = () => RuleId.Create(longId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 100 characters*");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Arrange
        var idWithWhitespace = "  RULE_001  ";

        // Act
        var ruleId = RuleId.Create(idWithWhitespace);

        // Assert
        ruleId.Value.Should().Be("RULE_001");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var ruleId1 = RuleId.Create("RULE_001");
        var ruleId2 = RuleId.Create("RULE_001");

        // Act & Assert
        ruleId1.Equals(ruleId2).Should().BeTrue();
        (ruleId1 == ruleId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCase_ShouldReturnTrue()
    {
        // Arrange
        var ruleId1 = RuleId.Create("rule_001");
        var ruleId2 = RuleId.Create("RULE_001");

        // Act & Assert
        ruleId1.Equals(ruleId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var ruleId1 = RuleId.Create("RULE_001");
        var ruleId2 = RuleId.Create("RULE_002");

        // Act & Assert
        ruleId1.Equals(ruleId2).Should().BeFalse();
        (ruleId1 != ruleId2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var ruleId1 = RuleId.Create("RULE_001");
        var ruleId2 = RuleId.Create("RULE_001");

        // Act & Assert
        ruleId1.GetHashCode().Should().Be(ruleId2.GetHashCode());
    }
}
