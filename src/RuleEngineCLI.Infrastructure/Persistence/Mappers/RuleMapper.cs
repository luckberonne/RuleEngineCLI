using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Persistence.Models;

namespace RuleEngineCLI.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper para convertir entre modelos de persistencia y entidades de dominio.
/// Aplica patrón Mapper/Adapter para separar concerns.
/// </summary>
public static class RuleMapper
{
    public static Rule ToDomain(RuleJsonModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        try
        {
            var ruleId = RuleId.Create(model.Id);
            var expression = Domain.ValueObjects.Expression.Create(model.Expression);
            var severity = Severity.FromString(model.Severity);

            return Rule.Create(
                ruleId,
                model.Description,
                expression,
                severity,
                model.ErrorMessage,
                model.IsEnabled);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to map rule with ID '{model.Id}' to domain model. {ex.Message}", ex);
        }
    }

    public static RuleJsonModel ToJsonModel(Rule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        return new RuleJsonModel
        {
            Id = rule.Id.Value,
            Description = rule.Description,
            Expression = rule.Expression.Value,
            Severity = rule.Severity.ToString(),
            ErrorMessage = rule.ErrorMessage,
            IsEnabled = rule.IsEnabled
        };
    }

    public static IEnumerable<Rule> ToDomainList(IEnumerable<RuleJsonModel> models)
    {
        if (models == null)
            throw new ArgumentNullException(nameof(models));

        var rules = new List<Rule>();
        var errors = new List<string>();

        foreach (var model in models)
        {
            try
            {
                rules.Add(ToDomain(model));
            }
            catch (Exception ex)
            {
                errors.Add($"Rule '{model?.Id ?? "unknown"}': {ex.Message}");
            }
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Failed to map {errors.Count} rule(s):\n{string.Join("\n", errors)}");
        }

        return rules;
    }
}
