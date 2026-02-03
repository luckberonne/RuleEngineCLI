using System.Globalization;
using System.Text.RegularExpressions;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;

namespace RuleEngineCLI.Infrastructure.Evaluation;

/// <summary>
/// Evaluador de expresiones que soporta comparaciones básicas.
/// Implementa patrón Strategy para evaluación de reglas.
/// IMPORTANTE: En producción considerar usar NCalc o similar para expresiones complejas.
/// Esta implementación es educativa y demuestra el concepto sin librerías externas.
/// </summary>
public sealed class ComparisonExpressionEvaluator : IExpressionEvaluator
{
    private static readonly Regex ComparisonPattern = new(
        @"^(\w+)\s*(==|!=|>=|<=|>|<)\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LogicalAndPattern = new(
        @"(.+?)\s*&&\s*(.+)",
        RegexOptions.Compiled);

    private static readonly Regex LogicalOrPattern = new(
        @"(.+?)\s*\|\|\s*(.+)",
        RegexOptions.Compiled);

    public bool CanEvaluate(Rule rule)
    {
        if (rule == null) return false;
        
        var expression = rule.Expression.Value;
        
        // Verifica si es una expresión de comparación simple o con operadores lógicos
        return ComparisonPattern.IsMatch(expression) ||
               LogicalAndPattern.IsMatch(expression) ||
               LogicalOrPattern.IsMatch(expression);
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

        var expression = rule.Expression.Value;
        var result = EvaluateExpression(expression, input);
        
        return Task.FromResult(result);
    }

    private bool EvaluateExpression(string expression, ValidationInputDto input)
    {
        // Manejar operador lógico AND (&&)
        var andMatch = LogicalAndPattern.Match(expression);
        if (andMatch.Success)
        {
            var left = andMatch.Groups[1].Value.Trim();
            var right = andMatch.Groups[2].Value.Trim();
            return EvaluateExpression(left, input) && EvaluateExpression(right, input);
        }

        // Manejar operador lógico OR (||)
        var orMatch = LogicalOrPattern.Match(expression);
        if (orMatch.Success)
        {
            var left = orMatch.Groups[1].Value.Trim();
            var right = orMatch.Groups[2].Value.Trim();
            return EvaluateExpression(left, input) || EvaluateExpression(right, input);
        }

        // Evaluar comparación simple
        var match = ComparisonPattern.Match(expression);
        if (!match.Success)
            throw new NotSupportedException($"Expression format not supported: {expression}");

        var propertyName = match.Groups[1].Value.Trim();
        var operatorSymbol = match.Groups[2].Value.Trim();
        var expectedValueStr = match.Groups[3].Value.Trim();

        // Obtener valor de la propiedad
        if (!input.HasProperty(propertyName))
            throw new InvalidOperationException($"Property '{propertyName}' not found in input data.");

        var actualValue = input.Properties[propertyName];

        // Evaluar la comparación
        return EvaluateComparison(actualValue, operatorSymbol, expectedValueStr);
    }

    private bool EvaluateComparison(object? actualValue, string operatorSymbol, string expectedValueStr)
    {
        // Limpiar comillas del valor esperado
        expectedValueStr = expectedValueStr.Trim('"', '\'');

        // Caso especial: comparación con null
        if (expectedValueStr.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return operatorSymbol switch
            {
                "==" => actualValue == null,
                "!=" => actualValue != null,
                _ => throw new NotSupportedException($"Operator '{operatorSymbol}' not supported for null comparison.")
            };
        }

        if (actualValue == null)
        {
            return operatorSymbol == "!=";
        }

        // Intentar comparación numérica
        if (TryParseNumeric(actualValue.ToString(), out var actualNumeric) &&
            TryParseNumeric(expectedValueStr, out var expectedNumeric))
        {
            return operatorSymbol switch
            {
                "==" => Math.Abs(actualNumeric - expectedNumeric) < 0.0001,
                "!=" => Math.Abs(actualNumeric - expectedNumeric) >= 0.0001,
                ">" => actualNumeric > expectedNumeric,
                "<" => actualNumeric < expectedNumeric,
                ">=" => actualNumeric >= expectedNumeric,
                "<=" => actualNumeric <= expectedNumeric,
                _ => throw new NotSupportedException($"Operator '{operatorSymbol}' not supported.")
            };
        }

        // Intentar comparación de fechas
        if (TryParseDate(actualValue.ToString(), out var actualDate) &&
            TryParseDate(expectedValueStr, out var expectedDate))
        {
            return operatorSymbol switch
            {
                "==" => actualDate == expectedDate,
                "!=" => actualDate != expectedDate,
                ">" => actualDate > expectedDate,
                "<" => actualDate < expectedDate,
                ">=" => actualDate >= expectedDate,
                "<=" => actualDate <= expectedDate,
                _ => throw new NotSupportedException($"Operator '{operatorSymbol}' not supported.")
            };
        }

        // Comparación de cadenas (case-insensitive por defecto)
        var actualStr = actualValue.ToString() ?? string.Empty;
        var comparison = StringComparison.OrdinalIgnoreCase;

        return operatorSymbol switch
        {
            "==" => actualStr.Equals(expectedValueStr, comparison),
            "!=" => !actualStr.Equals(expectedValueStr, comparison),
            ">" => string.Compare(actualStr, expectedValueStr, comparison) > 0,
            "<" => string.Compare(actualStr, expectedValueStr, comparison) < 0,
            ">=" => string.Compare(actualStr, expectedValueStr, comparison) >= 0,
            "<=" => string.Compare(actualStr, expectedValueStr, comparison) <= 0,
            _ => throw new NotSupportedException($"Operator '{operatorSymbol}' not supported.")
        };
    }

    private bool TryParseNumeric(string? value, out double result)
    {
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private bool TryParseDate(string? value, out DateTime result)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}
