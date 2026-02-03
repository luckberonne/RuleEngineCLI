using NCalc;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using System.Text.RegularExpressions;

namespace RuleEngineCLI.Infrastructure.Evaluation;

/// <summary>
/// Evaluador de expresiones usando NCalc que soporta matemáticas complejas y funciones.
/// Más potente que ComparisonExpressionEvaluator pero requiere expresiones compatibles con NCalc.
/// </summary>
public sealed class NCalcExpressionEvaluator : IExpressionEvaluator
{
    private static readonly Regex SafetyPattern = new(
        @"\b(System|Reflection|Process|File|IO|Assembly)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanEvaluate(Rule rule)
    {
        if (rule == null)
            return false;

        var expression = rule.Expression.Value;
        
        // Verificar que no contenga palabras clave peligrosas
        if (SafetyPattern.IsMatch(expression))
            return false;

        try
        {
            // Intentar crear la expresión para validar sintaxis
            _ = new Expression(expression, EvaluateOptions.IgnoreCase);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> EvaluateAsync(
        Rule rule,
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        if (input == null)
            throw new ArgumentNullException(nameof(input));

        try
        {
            var expression = new Expression(rule.Expression.Value, EvaluateOptions.IgnoreCase);

            // Inyectar todos los parámetros del input
            foreach (var property in input.Properties)
            {
                expression.Parameters[property.Key] = property.Value;
            }

            // Evaluar la expresión
            var result = expression.Evaluate();

            // Convertir el resultado a booleano
            return Task.FromResult(ConvertToBoolean(result));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate expression for rule {rule.Id}: {ex.Message}", ex);
        }
    }

    private static bool ConvertToBoolean(object? result)
    {
        if (result == null)
            return false;

        if (result is bool boolValue)
            return boolValue;

        if (result is int intValue)
            return intValue != 0;

        if (result is double doubleValue)
            return Math.Abs(doubleValue) > 0.0001;

        if (result is string stringValue)
            return !string.IsNullOrEmpty(stringValue) && 
                   !stringValue.Equals("false", StringComparison.OrdinalIgnoreCase);

        return true;
    }
}
