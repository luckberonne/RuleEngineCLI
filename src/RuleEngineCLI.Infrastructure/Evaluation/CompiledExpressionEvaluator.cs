using System.Collections.Concurrent;
using System.Linq.Expressions;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;

namespace RuleEngineCLI.Infrastructure.Evaluation;

/// <summary>
/// Evaluador de expresiones optimizado con compilación de Expression Trees (Phase 4).
/// Compila expresiones a delegates nativos para máxima performance.
/// Usa cache para evitar recompilar la misma expresión múltiples veces.
/// 
/// Performance: ~10-100x más rápido que parsing en cada evaluación.
/// </summary>
public sealed class CompiledExpressionEvaluator : IExpressionEvaluator
{
    private readonly ConcurrentDictionary<string, Func<ValidationInputDto, bool>> _compiledCache = new();

    public bool CanEvaluate(Rule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Expression.Value))
            return false;

        var expr = rule.Expression.Value.Trim();

        // Soporta expresiones de comparación simples y lógicas
        return expr.Contains("==") || expr.Contains("!=") ||
               expr.Contains(">=") || expr.Contains("<=") ||
               expr.Contains(">") || expr.Contains("<") ||
               expr.Contains("&&") || expr.Contains("||");
    }

    public bool Evaluate(Rule rule, ValidationInputDto input)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        if (input == null)
            throw new ArgumentNullException(nameof(input));

        try
        {
            // Obtener o compilar la expresión
            var compiledFunc = _compiledCache.GetOrAdd(
                rule.Expression.Value,
                expr => CompileExpression(expr));

            return compiledFunc(input);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate compiled expression for rule {rule.Id.Value}: {ex.Message}", ex);
        }
    }

    public Task<bool> EvaluateAsync(Rule rule, ValidationInputDto input, CancellationToken cancellationToken = default)
    {
        var result = Evaluate(rule, input);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Compila una expresión string a un delegate usando Expression Trees.
    /// </summary>
    private Func<ValidationInputDto, bool> CompileExpression(string expression)
    {
        // Parámetro de entrada: ValidationInputDto input
        var inputParam = Expression.Parameter(typeof(ValidationInputDto), "input");

        // Construir el árbol de expresión
        var expressionTree = BuildExpressionTree(expression, inputParam);

        // Compilar a delegate
        var lambda = Expression.Lambda<Func<ValidationInputDto, bool>>(expressionTree, inputParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Construye un árbol de expresión desde una string de expresión.
    /// Soporta: ==, !=, >, <, >=, <=, &&, ||
    /// </summary>
    private Expression BuildExpressionTree(string expression, ParameterExpression inputParam)
    {
        // Procesar operadores lógicos primero (menor precedencia)
        if (expression.Contains("||"))
        {
            var parts = expression.Split(new[] { "||" }, StringSplitOptions.None);
            var left = BuildExpressionTree(parts[0].Trim(), inputParam);
            var right = BuildExpressionTree(string.Join("||", parts.Skip(1)).Trim(), inputParam);
            return Expression.OrElse(left, right);
        }

        if (expression.Contains("&&"))
        {
            var parts = expression.Split(new[] { "&&" }, StringSplitOptions.None);
            var left = BuildExpressionTree(parts[0].Trim(), inputParam);
            var right = BuildExpressionTree(string.Join("&&", parts.Skip(1)).Trim(), inputParam);
            return Expression.AndAlso(left, right);
        }

        // Operadores de comparación
        return BuildComparisonExpression(expression, inputParam);
    }

    /// <summary>
    /// Construye una expresión de comparación (==, !=, >, <, >=, <=).
    /// </summary>
    private Expression BuildComparisonExpression(string expression, ParameterExpression inputParam)
    {
        // Detectar operador
        string[] operators = { "==", "!=", ">=", "<=", ">", "<" };
        string? foundOperator = null;
        int operatorIndex = -1;

        foreach (var op in operators)
        {
            var idx = expression.IndexOf(op, StringComparison.Ordinal);
            if (idx >= 0)
            {
                foundOperator = op;
                operatorIndex = idx;
                break;
            }
        }

        if (foundOperator == null)
            throw new InvalidOperationException($"No valid comparison operator found in: {expression}");

        var leftStr = expression.Substring(0, operatorIndex).Trim();
        var rightStr = expression.Substring(operatorIndex + foundOperator.Length).Trim();

        // Construir expresiones izquierda y derecha
        var leftExpr = BuildValueExpression(leftStr, inputParam);
        var rightExpr = BuildValueExpression(rightStr, inputParam);

        // Convertir a tipo común si es necesario
        if (leftExpr.Type != rightExpr.Type)
        {
            if (leftExpr.Type == typeof(object))
                leftExpr = Expression.Convert(leftExpr, rightExpr.Type);
            else if (rightExpr.Type == typeof(object))
                rightExpr = Expression.Convert(rightExpr, leftExpr.Type);
        }

        // Crear expresión de comparación
        return foundOperator switch
        {
            "==" => Expression.Equal(leftExpr, rightExpr),
            "!=" => Expression.NotEqual(leftExpr, rightExpr),
            ">" => Expression.GreaterThan(leftExpr, rightExpr),
            "<" => Expression.LessThan(leftExpr, rightExpr),
            ">=" => Expression.GreaterThanOrEqual(leftExpr, rightExpr),
            "<=" => Expression.LessThanOrEqual(leftExpr, rightExpr),
            _ => throw new InvalidOperationException($"Unsupported operator: {foundOperator}")
        };
    }

    /// <summary>
    /// Construye una expresión para un valor (campo o literal).
    /// </summary>
    private Expression BuildValueExpression(string value, ParameterExpression inputParam)
    {
        // Literal string
        if (value.StartsWith('"') && value.EndsWith('"'))
        {
            var stringValue = value.Trim('"');
            return Expression.Constant(stringValue, typeof(string));
        }

        // Literal boolean
        if (bool.TryParse(value, out var boolValue))
            return Expression.Constant(boolValue, typeof(bool));

        // Literal numérico
        if (int.TryParse(value, out var intValue))
            return Expression.Constant(intValue, typeof(int));

        if (double.TryParse(value, out var doubleValue))
            return Expression.Constant(doubleValue, typeof(double));

        // Campo del input (llamar a GetValue<object>)
        var getValueMethod = typeof(ValidationInputDto).GetMethod(nameof(ValidationInputDto.GetValue))!
            .MakeGenericMethod(typeof(object));

        var fieldName = Expression.Constant(value, typeof(string));
        var getValueCall = Expression.Call(inputParam, getValueMethod, fieldName);

        return getValueCall;
    }

    /// <summary>
    /// Obtiene estadísticas del cache de expresiones compiladas.
    /// </summary>
    public int GetCacheSize() => _compiledCache.Count;

    /// <summary>
    /// Limpia el cache de expresiones compiladas.
    /// </summary>
    public void ClearCache() => _compiledCache.Clear();
}
