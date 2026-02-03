using Microsoft.AspNetCore.Mvc;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineAppLogger = RuleEngineCLI.Application.Services.ILogger;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;
using System.Text.Json;

namespace RuleEngineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public RulesController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Validates input data against business rules
    /// </summary>
    /// <param name="request">The validation request containing rules file path and input data</param>
    /// <returns>Validation report with results and any violations</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate([FromBody] ValidationRequest request)
    {
        try
        {
            var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(request.InputJson)
                ?? throw new ArgumentException("Invalid input JSON");

            var inputDto = new ValidationInputDto(input);

            // Create rule engine with specific rules file
            var ruleRepository = new JsonRuleRepository(request.RulesFilePath);
            var expressionEvaluator = _serviceProvider.GetRequiredService<IExpressionEvaluator>();
            var logger = _serviceProvider.GetRequiredService<RuleEngineAppLogger>();
            var ruleEngine = new RuleEngineCLI.Application.Implementation.RuleEngine(ruleRepository, expressionEvaluator, logger);

            var result = await ruleEngine.EvaluateEnabledRulesAsync(inputDto);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validates input data against business rules with custom configuration
    /// </summary>
    /// <param name="request">The validation request with configuration options</param>
    /// <returns>Validation report with results and any violations</returns>
    [HttpPost("validate/advanced")]
    [ProducesResponseType(typeof(ValidationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateAdvanced([FromBody] AdvancedValidationRequest request)
    {
        try
        {
            var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(request.InputJson)
                ?? throw new ArgumentException("Invalid input JSON");

            var inputDto = new ValidationInputDto(input);

            // Create rule engine with specific rules file
            var ruleRepository = new JsonRuleRepository(request.RulesFilePath);
            var expressionEvaluator = _serviceProvider.GetRequiredService<IExpressionEvaluator>();
            var logger = _serviceProvider.GetRequiredService<RuleEngineAppLogger>();
            var ruleEngine = new RuleEngineCLI.Application.Implementation.RuleEngine(ruleRepository, expressionEvaluator, logger);

            var result = request.OnlyEnabledRules
                ? await ruleEngine.EvaluateEnabledRulesAsync(inputDto)
                : await ruleEngine.EvaluateAsync(inputDto);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record ValidationRequest(string RulesFilePath, string InputJson);

public record AdvancedValidationRequest(
    string RulesFilePath,
    string InputJson,
    bool OnlyEnabledRules = true);