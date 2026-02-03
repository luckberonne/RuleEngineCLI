using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.Implementation;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

namespace RuleEngineCLI.Presentation.CLI.DependencyInjection;

/// <summary>
/// Configuración del contenedor de Dependency Injection.
/// Aplica SOLID: DIP - todas las dependencias se resuelven mediante abstracciones.
/// Composition Root pattern: único lugar donde se crean instancias concretas.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceProvider BuildServiceProvider(string rulesFilePath, LogLevel logLevel = LogLevel.Information)
    {
        var services = new ServiceCollection();

        // Infrastructure Layer - Logging
        services.AddSingleton<ILogger>(sp => new ConsoleLogger(logLevel, includeTimestamp: true));

        // Infrastructure Layer - Repositories
        services.AddSingleton<IRuleRepository>(sp => new JsonRuleRepository(rulesFilePath));

        // Infrastructure Layer - Expression Evaluator
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();

        // Application Layer - Rule Engine
        services.AddSingleton<IRuleEngine, RuleEngine>();

        return services.BuildServiceProvider();
    }
}
