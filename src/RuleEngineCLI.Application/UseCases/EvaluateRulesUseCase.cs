using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;

namespace RuleEngineCLI.Application.UseCases;

/// <summary>
/// Caso de uso principal: Evaluar reglas contra datos de entrada.
/// Aplica SOLID:
/// - SRP: única responsabilidad de orquestar la evaluación de reglas
/// - DIP: depende de abstracciones (interfaces), no de implementaciones concretas
/// - OCP: cerrado a modificación, abierto a extensión mediante inyección de dependencias
/// </summary>
public sealed class EvaluateRulesUseCase
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger _logger;

    public EvaluateRulesUseCase(
        IRuleRepository ruleRepository,
        IExpressionEvaluator expressionEvaluator,
        ILogger logger)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta la evaluación completa de reglas.
    /// </summary>
    public async Task<ValidationReportDto> ExecuteAsync(
        ValidationInputDto input,
        bool onlyEnabledRules = true,
        CancellationToken cancellationToken = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        _logger.LogInformation("Starting rule evaluation process...");

        try
        {
            // 1. Cargar reglas desde el repositorio
            var rules = onlyEnabledRules
                ? await _ruleRepository.LoadEnabledRulesAsync(cancellationToken)
                : await _ruleRepository.LoadAllRulesAsync(cancellationToken);

            var rulesList = rules.ToList();
            _logger.LogInformation($"Loaded {rulesList.Count} rules for evaluation.");

            if (rulesList.Count == 0)
            {
                _logger.LogWarning("No rules found to evaluate.");
                return ValidationReportDto.FromDomain(ValidationReport.Create());
            }

            // 2. Crear el reporte de validación
            var report = ValidationReport.Create();

            // 3. Evaluar cada regla
            foreach (var rule in rulesList)
            {
                try
                {
                    _logger.LogDebug($"Evaluating rule: {rule.Id}");

                    // Verificar si el evaluador puede manejar esta regla
                    if (!_expressionEvaluator.CanEvaluate(rule))
                    {
                        _logger.LogWarning($"Rule {rule.Id} cannot be evaluated by current evaluator. Skipping.");
                        continue;
                    }

                    // Evaluar la expresión
                    var passed = await _expressionEvaluator.EvaluateAsync(rule, input, cancellationToken);

                    // Crear resultado
                    var result = passed
                        ? RuleResult.Success(rule)
                        : RuleResult.Failure(rule);

                    report.AddResult(result);

                    var status = passed ? "PASSED" : "FAILED";
                    _logger.LogDebug($"Rule {rule.Id}: {status}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error evaluating rule {rule.Id}: {ex.Message}", ex);
                    
                    // En caso de error de evaluación, tratar como fallo
                    var errorResult = RuleResult.Failure(rule, $"Evaluation error: {ex.Message}");
                    report.AddResult(errorResult);
                }
            }

            // 4. Generar reporte final
            _logger.LogInformation($"Evaluation completed. Status: {report.Status}");
            _logger.LogInformation($"Total: {report.TotalRulesEvaluated}, Passed: {report.TotalPassed}, Failed: {report.TotalFailed}");

            return ValidationReportDto.FromDomain(report);
        }
        catch (Exception ex)
        {
            _logger.LogError("Fatal error during rule evaluation process.", ex);
            throw new RuleEvaluationException("Failed to evaluate rules. See inner exception for details.", ex);
        }
    }
}

/// <summary>
/// Excepción personalizada para errores en el proceso de evaluación.
/// </summary>
public class RuleEvaluationException : Exception
{
    public RuleEvaluationException(string message) : base(message) { }
    public RuleEvaluationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
