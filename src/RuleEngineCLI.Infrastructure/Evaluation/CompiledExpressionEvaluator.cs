using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
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

    private static readonly MethodInfo CompareValuesMethod = typeof(CompiledExpressionEvaluator)
        .GetMethod(nameof(CompareValues), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Construye una expresión de comparación (==, !=, >, <, >=, <=).
    /// Delega la comparación real a <see cref="CompareValues"/> en lugar de generar
    /// operadores tipados: los valores de entrada llegan como object (JSON deserializado,
    /// típicamente double) y no se puede saber su tipo real en tiempo de compilación.
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

        var leftExpr = BuildValueExpression(leftStr, inputParam);
        var rightExpr = BuildValueExpression(rightStr, inputParam);

        return Expression.Call(
            CompareValuesMethod,
            Expression.Convert(leftExpr, typeof(object)),
            Expression.Constant(foundOperator),
            Expression.Convert(rightExpr, typeof(object)));
    }

    /// <summary>
    /// Compara dos valores en tiempo de ejecución probando numérico, luego fecha, luego string.
    /// Misma semántica que ComparisonExpressionEvaluator, para que ambos evaluadores se comporten igual.
    /// </summary>
    private static bool CompareValues(object? left, string op, object? right)
    {
        if (left == null || right == null)
        {
            return op switch
            {
                "==" => Equals(left, right),
                "!=" => !Equals(left, right),
                _ => throw new NotSupportedException($"Operator '{op}' not supported for null comparison.")
            };
        }

        if (TryToDouble(left, out var leftNum) && TryToDouble(right, out var rightNum))
        {
            return op switch
            {
                "==" => Math.Abs(leftNum - rightNum) < 0.0001,
                "!=" => Math.Abs(leftNum - rightNum) >= 0.0001,
                ">" => leftNum > rightNum,
                "<" => leftNum < rightNum,
                ">=" => leftNum >= rightNum,
                "<=" => leftNum <= rightNum,
                _ => throw new NotSupportedException($"Operator '{op}' not supported.")
            };
        }

        if (TryToDate(left, out var leftDate) && TryToDate(right, out var rightDate))
        {
            return op switch
            {
                "==" => leftDate == rightDate,
                "!=" => leftDate != rightDate,
                ">" => leftDate > rightDate,
                "<" => leftDate < rightDate,
                ">=" => leftDate >= rightDate,
                "<=" => leftDate <= rightDate,
                _ => throw new NotSupportedException($"Operator '{op}' not supported.")
            };
        }

        var comparison = string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);

        return op switch
        {
            "==" => comparison == 0,
            "!=" => comparison != 0,
            ">" => comparison > 0,
            "<" => comparison < 0,
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            _ => throw new NotSupportedException($"Operator '{op}' not supported.")
        };
    }

    private static bool TryToDouble(object value, out double result)
        => double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static bool TryToDate(object value, out DateTime result)
        => DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

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

        // Ni número, ni bool, ni string entre comillas: puede ser un campo del input
        // o un literal de texto sin comillas (p.ej. "status == active"). Se resuelve
        // en runtime contra el input real, igual que ComparisonExpressionEvaluator.
        return Expression.Call(ResolveFieldMethod, inputParam, Expression.Constant(value, typeof(string)));
    }

    private static readonly MethodInfo ResolveFieldMethod = typeof(CompiledExpressionEvaluator)
        .GetMethod(nameof(ResolveField), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static object? ResolveField(ValidationInputDto input, string token)
        => input.HasProperty(token) ? input.Properties[token] : token;

    /// <summary>
    /// Obtiene estadísticas del cache de expresiones compiladas.
    /// </summary>
    public int GetCacheSize() => _compiledCache.Count;

    /// <summary>
    /// Limpia el cache de expresiones compiladas.
    /// </summary>
    public void ClearCache() => _compiledCache.Clear();
}
