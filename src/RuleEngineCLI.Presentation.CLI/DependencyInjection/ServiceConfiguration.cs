using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.Configuration;
using RuleEngineCLI.Application.Implementation;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Monitoring;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

namespace RuleEngineCLI.Presentation.CLI.DependencyInjection;

/// <summary>
/// Configuración del contenedor de Dependency Injection.
/// Aplica SOLID: DIP - todas las dependencias se resuelven mediante abstracciones.
/// Composition Root pattern: único lugar donde se crean instancias concretas.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceProvider BuildServiceProvider(RuleEngineOptions options, LogLevel logLevel = LogLevel.Information)
    {
        var services = new ServiceCollection();

        // Infrastructure Layer - Logging
        services.AddSingleton<ILogger>(sp => new ConsoleLogger(logLevel, includeTimestamp: true));

        // Infrastructure Layer - Repositories (con caché opcional, ver EvaluationOptions/CacheOptions)
        services.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
        services.AddSingleton<IRuleRepository>(sp =>
        {
            IRuleRepository repository = new JsonRuleRepository(options.RulesFilePath);

            if (options.Cache.Enabled)
            {
                repository = new CachedRuleRepository(
                    repository,
                    sp.GetRequiredService<IMemoryCache>(),
                    TimeSpan.FromMinutes(options.Cache.ExpirationMinutes));
            }

            return repository;
        });

        // Infrastructure Layer - Expression Evaluator
        services.AddSingleton<IExpressionEvaluator>(sp =>
            options.Evaluation.EvaluatorType.Equals("Compiled", StringComparison.OrdinalIgnoreCase)
                ? new CompiledExpressionEvaluator()
                : new ComparisonExpressionEvaluator());

        // Application Layer - Rule Engine (secuencial o paralelo, con métricas opcionales)
        services.AddSingleton<IRuleEngine>(sp =>
        {
            var repository = sp.GetRequiredService<IRuleRepository>();
            var evaluator = sp.GetRequiredService<IExpressionEvaluator>();
            var logger = sp.GetRequiredService<ILogger>();

            IRuleEngine engine = options.Evaluation.Parallel
                ? new ParallelRuleEngine(repository, new[] { evaluator }, logger)
                : new RuleEngine(repository, evaluator, logger);

            return options.Evaluation.EnableMetrics
                ? new InstrumentedRuleEngine(engine)
                : engine;
        });

        return services.BuildServiceProvider();
    }
}
