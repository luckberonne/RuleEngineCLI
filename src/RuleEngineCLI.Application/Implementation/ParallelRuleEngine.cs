using System.Collections.Concurrent;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;

namespace RuleEngineCLI.Application.Implementation;

/// <summary>
/// Motor de reglas optimizado con evaluación paralela (Phase 4).
/// Usa Parallel.ForEach para evaluar múltiples reglas concurrentemente.
/// Mejora significativa en performance para conjuntos grandes de reglas.
/// </summary>
public sealed class ParallelRuleEngine : IRuleEngine
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IEnumerable<IExpressionEvaluator> _evaluators;
    private readonly ILogger _logger;
    private readonly ParallelOptions _parallelOptions;

    public ParallelRuleEngine(
        IRuleRepository ruleRepository,
        IEnumerable<IExpressionEvaluator> evaluators,
        ILogger logger,
        int maxDegreeOfParallelism = -1) // -1 = usar todos los cores disponibles
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _evaluators = evaluators ?? throw new ArgumentNullException(nameof(evaluators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = CancellationToken.None
        };
    }

    public async Task<ValidationReportDto> EvaluateAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        _logger.LogInformation("Starting parallel rule evaluation...");

        var rules = await _ruleRepository.LoadAllRulesAsync(cancellationToken);
        return await EvaluateRulesInParallel(rules, input, cancellationToken);
    }

    public async Task<ValidationReportDto> EvaluateEnabledRulesAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        _logger.LogInformation("Starting parallel evaluation of enabled rules...");

        var rules = await _ruleRepository.LoadEnabledRulesAsync(cancellationToken);
        return await EvaluateRulesInParallel(rules, input, cancellationToken);
    }

    private Task<ValidationReportDto> EvaluateRulesInParallel(
        IEnumerable<Rule> rules,
        ValidationInputDto input,
        CancellationToken cancellationToken)
    {
        var rulesList = rules.ToList();
        _logger.LogInformation($"Loaded {rulesList.Count} rules for parallel evaluation.");

        if (rulesList.Count == 0)
        {
            _logger.LogWarning("No rules found to evaluate.");
            return Task.FromResult(ValidationReportDto.FromDomain(ValidationReport.Create()));
        }

        // ConcurrentBag es thread-safe para agregar resultados desde múltiples threads
        var results = new ConcurrentBag<RuleResult>();
        var report = ValidationReport.Create();

        // Actualizar las opciones con el token de cancelación
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _parallelOptions.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        try
        {
            // Evaluación paralela de reglas
            Parallel.ForEach(rulesList, parallelOptions, rule =>
            {
                try
                {
                    _logger.LogDebug($"[Thread {Environment.CurrentManagedThreadId}] Evaluating rule: {rule.Id}");

                    // Buscar el evaluador apropiado para esta regla
                    var evaluator = _evaluators.FirstOrDefault(e => e.CanEvaluate(rule));

                    if (evaluator == null)
                    {
                        _logger.LogWarning($"No evaluator found for rule: {rule.Id}");
                        var errorResult = RuleResult.Failure(rule, 
                            $"No evaluator available for expression: {rule.Expression.Value}");
                        results.Add(errorResult);
                        return;
                    }

                    // Evaluar la regla - necesitamos evaluar de forma sincrónica
                    // IExpressionEvaluator solo tiene EvaluateAsync, lo usamos con .Result
                    var passed = evaluator.EvaluateAsync(rule, input, cancellationToken).Result;

                    var result = passed
                        ? RuleResult.Success(rule)
                        : RuleResult.Failure(rule);

                    results.Add(result);

                    _logger.LogDebug($"[Thread {Environment.CurrentManagedThreadId}] Rule {rule.Id}: {(passed ? "PASSED" : "FAILED")}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule {rule.Id}: {ex.Message}");
                    var errorResult = RuleResult.Failure(rule, $"Evaluation error: {ex.Message}");
                    results.Add(errorResult);
                }
            });

            // Agregar resultados al reporte (order by rule ID para consistencia)
            foreach (var result in results.OrderBy(r => r.RuleId.Value))
            {
                report.AddResult(result);
            }

            _logger.LogInformation($"Parallel evaluation completed. Total: {report.TotalRulesEvaluated}, " +
                                 $"Passed: {report.TotalPassed}, Failed: {report.TotalFailed}");

            return Task.FromResult(ValidationReportDto.FromDomain(report));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Parallel rule evaluation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fatal error during parallel evaluation: {ex.Message}");
            throw new InvalidOperationException("Failed to evaluate rules in parallel", ex);
        }
    }
}
