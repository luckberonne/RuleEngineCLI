using FluentAssertions;
using RuleEngineCLI.Domain.ValueObjects;
using Xunit;

namespace RuleEngineCLI.Domain.Tests.ValueObjects;

public class SeverityTests
{
    [Theory]
    [InlineData("INFO")]
    [InlineData("info")]
    [InlineData("Info")]
    public void FromString_WithValidInfo_ShouldReturnInfoSeverity(string value)
    {
        // Act
        var severity = Severity.FromString(value);

        // Assert
        severity.Should().Be(Severity.Info);
        severity.Level.Should().Be(SeverityLevel.Info);
    }

    [Theory]
    [InlineData("WARN")]
    [InlineData("WARNING")]
    [InlineData("warn")]
    public void FromString_WithValidWarning_ShouldReturnWarningSeverity(string value)
    {
        // Act
        var severity = Severity.FromString(value);

        // Assert
        severity.Should().Be(Severity.Warning);
        severity.Level.Should().Be(SeverityLevel.Warning);
    }

    [Theory]
    [InlineData("ERROR")]
    [InlineData("error")]
    public void FromString_WithValidError_ShouldReturnErrorSeverity(string value)
    {
        // Act
        var severity = Severity.FromString(value);

        // Assert
        severity.Should().Be(Severity.Error);
        severity.Level.Should().Be(SeverityLevel.Error);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData(null)]
    public void FromString_WithInvalidValue_ShouldThrowArgumentException(string? invalidValue)
    {
        // Act
        Action act = () => Severity.FromString(invalidValue!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid severity value*");
    }

    [Fact]
    public void CompareTo_InfoLessThanWarning()
    {
        // Arrange
        var info = Severity.Info;
        var warning = Severity.Warning;

        // Act & Assert
        (info < warning).Should().BeTrue();
        info.CompareTo(warning).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_WarningLessThanError()
    {
        // Arrange
        var warning = Severity.Warning;
        var error = Severity.Error;

        // Act & Assert
        (warning < error).Should().BeTrue();
        warning.CompareTo(error).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_ErrorGreaterThanInfo()
    {
        // Arrange
        var error = Severity.Error;
        var info = Severity.Info;

        // Act & Assert
        (error > info).Should().BeTrue();
        error.CompareTo(info).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Equals_WithSameSeverity_ShouldReturnTrue()
    {
        // Arrange
        var severity1 = Severity.Error;
        var severity2 = Severity.Error;

        // Act & Assert
        severity1.Equals(severity2).Should().BeTrue();
        (severity1 == severity2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsUpperCaseString()
    {
        // Act & Assert
        Severity.Info.ToString().Should().Be("INFO");
        Severity.Warning.ToString().Should().Be("WARNING");
        Severity.Error.ToString().Should().Be("ERROR");
    }
}
