using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;

namespace RuleEngineCLI.Application.Implementation;

/// <summary>
/// Implementación del motor de reglas que orquesta la evaluación.
/// Aplica Facade Pattern para simplificar el uso desde la capa de presentación.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger _logger;

    public RuleEngine(
        IRuleRepository ruleRepository,
        IExpressionEvaluator expressionEvaluator,
        ILogger logger)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationReportDto> EvaluateAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        var useCase = new UseCases.EvaluateRulesUseCase(
            _ruleRepository,
            _expressionEvaluator,
            _logger);

        return await useCase.ExecuteAsync(input, onlyEnabledRules: false, cancellationToken);
    }

    public async Task<ValidationReportDto> EvaluateEnabledRulesAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        var useCase = new UseCases.EvaluateRulesUseCase(
            _ruleRepository,
            _expressionEvaluator,
            _logger);

        return await useCase.ExecuteAsync(input, onlyEnabledRules: true, cancellationToken);
    }
}
