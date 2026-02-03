using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;

namespace RuleEngineCLI.Infrastructure.Evaluation;

/// <summary>
/// Evaluador de expresiones con operadores avanzados.
/// Soporta: RegEx, In, NotIn, Between, IsNull, IsNotNull, StartsWith, EndsWith, Contains
/// </summary>
public class AdvancedOperatorsEvaluator : IExpressionEvaluator
{
    private static readonly HashSet<string> SupportedOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "RegEx", "Regex", "Matches",
        "In", "NotIn",
        "Between",
        "IsNull", "IsNotNull",
        "StartsWith", "EndsWith", "Contains"
    };

    public bool CanEvaluate(Rule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Expression.Value))
            return false;

        // Detectar si la expresión usa operadores avanzados
        var expression = rule.Expression.Value;
        return SupportedOperators.Any(op => expression.Contains(op, StringComparison.OrdinalIgnoreCase));
    }

    public Task<bool> EvaluateAsync(Rule rule, ValidationInputDto input, CancellationToken cancellationToken = default)
    {
        var result = Evaluate(rule, input);
        return Task.FromResult(result);
    }

    public bool Evaluate(Rule rule, ValidationInputDto input)
    {
        try
        {
            var expression = rule.Expression.Value.Trim();

            // RegEx: field RegEx pattern
            if (ContainsOperator(expression, "RegEx") || ContainsOperator(expression, "Regex") || ContainsOperator(expression, "Matches"))
            {
                return EvaluateRegEx(expression, input);
            }

            // String operators: field StartsWith/EndsWith/Contains value (check before In to avoid conflicts)
            if (ContainsOperator(expression, "StartsWith"))
                return EvaluateStartsWith(expression, input);

            if (ContainsOperator(expression, "EndsWith"))
                return EvaluateEndsWith(expression, input);

            // Check Contains last to avoid matching "Contains" in other operators
            if (ContainsOperator(expression, " Contains "))
                return EvaluateContains(expression, input);

            // In: field In [value1, value2, ...]
            if (ContainsOperator(expression, "NotIn"))
            {
                return EvaluateNotIn(expression, input);
            }
            
            if (ContainsOperator(expression, "In"))
            {
                return EvaluateIn(expression, input);
            }

            // Between: field Between min And max
            if (ContainsOperator(expression, "Between"))
            {
                return EvaluateBetween(expression, input);
            }

            // IsNull/IsNotNull: field IsNull or field IsNotNull
            if (ContainsOperator(expression, "IsNotNull"))
            {
                return EvaluateIsNotNull(expression, input);
            }
            
            if (ContainsOperator(expression, "IsNull"))
            {
                return EvaluateIsNull(expression, input);
            }

            throw new InvalidOperationException($"Unsupported operator in expression: {expression}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate expression for rule {rule.Id.Value}: {ex.Message}", ex);
        }
    }

    private bool ContainsOperator(string expression, string op)
    {
        return expression.Contains(op, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateRegEx(string expression, ValidationInputDto input)
    {
        // Format: field RegEx pattern
        var parts = expression.Split(new[] { "RegEx", "Regex", "Matches" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("RegEx expression must be: field RegEx pattern");

        var fieldName = parts[0].Trim();
        var pattern = parts[1].Trim().Trim('"', '\'');

        var value = input.GetValue<string>(fieldName);
        if (value == null)
            return false;

        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    private bool EvaluateIn(string expression, ValidationInputDto input)
    {
        // Format: field In [value1, value2, value3]
        var inIndex = expression.IndexOf("In", StringComparison.OrdinalIgnoreCase);
        var fieldName = expression.Substring(0, inIndex).Trim();
        var valuesPart = expression.Substring(inIndex + 2).Trim();

        if (!valuesPart.StartsWith('[') || !valuesPart.EndsWith(']'))
            throw new InvalidOperationException("In operator values must be enclosed in [brackets]");

        var valuesStr = valuesPart.Trim('[', ']');
        var allowedValues = valuesStr.Split(',')
            .Select(v => v.Trim().Trim('"', '\''))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fieldValue = input.GetValue<string>(fieldName);
        if (fieldValue == null)
            return false;

        return allowedValues.Contains(fieldValue);
    }

    private bool EvaluateNotIn(string expression, ValidationInputDto input)
    {
        // Format: field NotIn [value1, value2, value3]
        return !EvaluateIn(expression.Replace("NotIn", "In", StringComparison.OrdinalIgnoreCase), input);
    }

    private bool EvaluateBetween(string expression, ValidationInputDto input)
    {
        // Format: field Between min And max
        var betweenIndex = expression.IndexOf("Between", StringComparison.OrdinalIgnoreCase);
        var andIndex = expression.IndexOf("And", betweenIndex, StringComparison.OrdinalIgnoreCase);

        if (andIndex == -1)
            throw new InvalidOperationException("Between operator requires 'And' keyword");

        var fieldName = expression.Substring(0, betweenIndex).Trim();
        var minStr = expression.Substring(betweenIndex + 7, andIndex - betweenIndex - 7).Trim();
        var maxStr = expression.Substring(andIndex + 3).Trim();

        // Intentar obtener el valor como diferentes tipos numéricos
        if (!input.HasProperty(fieldName))
            return false;

        var rawValue = input.Properties[fieldName];
        if (rawValue == null)
            return false;

        double fieldValue;
        try
        {
            fieldValue = Convert.ToDouble(rawValue);
        }
        catch
        {
            return false;
        }

        if (!double.TryParse(minStr, out var min) || !double.TryParse(maxStr, out var max))
            throw new InvalidOperationException("Between operator requires numeric min and max values");

        return fieldValue >= min && fieldValue <= max;
    }

    private bool EvaluateIsNull(string expression, ValidationInputDto input)
    {
        // Format: field IsNull
        var fieldName = expression.Replace("IsNull", "", StringComparison.OrdinalIgnoreCase).Trim();
        
        if (!input.HasProperty(fieldName))
            return true;

        var value = input.Properties[fieldName];
        return value == null;
    }

    private bool EvaluateIsNotNull(string expression, ValidationInputDto input)
    {
        // Format: field IsNotNull
        return !EvaluateIsNull(expression.Replace("IsNotNull", "IsNull", StringComparison.OrdinalIgnoreCase), input);
    }

    private bool EvaluateStartsWith(string expression, ValidationInputDto input)
    {
        // Format: field StartsWith value
        var parts = expression.Split(new[] { "StartsWith" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("StartsWith expression must be: field StartsWith value");

        var fieldName = parts[0].Trim();
        var searchValue = parts[1].Trim().Trim('"', '\'');

        var fieldValue = input.GetValue<string>(fieldName);
        if (fieldValue == null)
            return false;

        return fieldValue.StartsWith(searchValue, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateEndsWith(string expression, ValidationInputDto input)
    {
        // Format: field EndsWith value
        var parts = expression.Split(new[] { "EndsWith" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("EndsWith expression must be: field EndsWith value");

        var fieldName = parts[0].Trim();
        var searchValue = parts[1].Trim().Trim('"', '\'');

        var fieldValue = input.GetValue<string>(fieldName);
        if (fieldValue == null)
            return false;

        return fieldValue.EndsWith(searchValue, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateContains(string expression, ValidationInputDto input)
    {
        // Format: field Contains value
        var parts = expression.Split(new[] { "Contains" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("Contains expression must be: field Contains value");

        var fieldName = parts[0].Trim();
        var searchValue = parts[1].Trim().Trim('"', '\'');

        var fieldValue = input.GetValue<string>(fieldName);
        if (fieldValue == null)
            return false;

        return fieldValue.Contains(searchValue, StringComparison.OrdinalIgnoreCase);
    }
}
