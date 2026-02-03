using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using RuleEngineCLI.Application.Configuration;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Monitoring;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;
using RuleEngineCLI.Infrastructure.Validation;

namespace RuleEngineCLI.ConfigurationExample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  RuleEngineCLI - Configuration Example (Phase 2)          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Determinar entorno
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        Console.WriteLine($"🌍 Environment: {environment}");
        Console.WriteLine();

        // Cargar configuración
        var configuration = BuildConfiguration(environment);
        var options = configuration.GetSection(RuleEngineOptions.SectionName).Get<RuleEngineOptions>()
                      ?? new RuleEngineOptions();

        // Mostrar configuración
        DisplayConfiguration(options);

        // Configurar servicios
        var serviceProvider = ConfigureServices(configuration, options);

        // Demo 1: Validación de esquema
        await DemoSchemaValidation(serviceProvider, options);

        // Demo 2: Logging estructurado con diferentes formatos
        await DemoStructuredLogging(serviceProvider);

        // Demo 3: Configuración multi-entorno
        DemoEnvironmentConfiguration(options, environment);
    }

    private static IConfiguration BuildConfiguration(string environment)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static IServiceProvider ConfigureServices(IConfiguration configuration, RuleEngineOptions options)
    {
        var services = new ServiceCollection();

        // Registrar configuración
        services.AddSingleton(configuration);
        services.AddSingleton(options);

        // Memory Cache
        services.AddMemoryCache();

        // Logger con configuración
        var logLevel = ParseLogLevel(options.Logging.MinimumLevel);
        var logFormat = ParseLogFormat(options.Logging.Format);
        services.AddSingleton<ILogger>(sp => new StructuredLogger(
            logLevel,
            options.Logging.IncludeTimestamp,
            options.Logging.IncludeExceptionDetails,
            logFormat));

        // Validador de esquemas
        services.AddSingleton<JsonSchemaValidator>();

        // Repositorio con o sin caché
        services.AddSingleton<IRuleRepository>(sp =>
        {
            var baseRepo = new JsonRuleRepository(options.RulesFilePath);
            
            if (options.Cache.Enabled)
            {
                var cache = sp.GetRequiredService<IMemoryCache>();
                var expiration = TimeSpan.FromMinutes(options.Cache.ExpirationMinutes);
                return new CachedRuleRepository(baseRepo, cache, expiration);
            }
            
            return baseRepo;
        });

        // Evaluador según configuración
        services.AddSingleton<IExpressionEvaluator>(sp =>
        {
            return options.Evaluation.EvaluatorType.ToLower() switch
            {
                "ncalc" => new NCalcExpressionEvaluator(),
                "comparison" => new ComparisonExpressionEvaluator(),
                _ => new ComparisonExpressionEvaluator()
            };
        });

        // Motor de reglas con o sin instrumentación
        services.AddSingleton<IRuleEngine>(sp =>
        {
            var repo = sp.GetRequiredService<IRuleRepository>();
            var evaluator = sp.GetRequiredService<IExpressionEvaluator>();
            var logger = sp.GetRequiredService<ILogger>();
            
            var baseEngine = new RuleEngineCLI.Application.Implementation.RuleEngine(
                repo, evaluator, logger);

            if (options.Evaluation.EnableMetrics)
            {
                return new InstrumentedRuleEngine(baseEngine);
            }

            return baseEngine;
        });

        return services.BuildServiceProvider();
    }

    private static void DisplayConfiguration(RuleEngineOptions options)
    {
        Console.WriteLine("⚙️  Configuration:");
        Console.WriteLine($"   Rules File: {options.RulesFilePath}");
        Console.WriteLine($"   Validate Schema: {options.ValidateSchema}");
        Console.WriteLine();
        Console.WriteLine("   Cache:");
        Console.WriteLine($"     Enabled: {options.Cache.Enabled}");
        Console.WriteLine($"     Expiration: {options.Cache.ExpirationMinutes} minutes");
        Console.WriteLine($"     Max Size: {options.Cache.MaxSize?.ToString() ?? "unlimited"}");
        Console.WriteLine();
        Console.WriteLine("   Logging:");
        Console.WriteLine($"     Level: {options.Logging.MinimumLevel}");
        Console.WriteLine($"     Format: {options.Logging.Format}");
        Console.WriteLine($"     Timestamp: {options.Logging.IncludeTimestamp}");
        Console.WriteLine();
        Console.WriteLine("   Evaluation:");
        Console.WriteLine($"     Evaluator: {options.Evaluation.EvaluatorType}");
        Console.WriteLine($"     Continue On Error: {options.Evaluation.ContinueOnError}");
        Console.WriteLine($"     Metrics: {options.Evaluation.EnableMetrics}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static async Task DemoSchemaValidation(IServiceProvider serviceProvider, RuleEngineOptions options)
    {
        Console.WriteLine("📋 Demo 1: JSON Schema Validation");
        Console.WriteLine();

        var validator = serviceProvider.GetRequiredService<JsonSchemaValidator>();
        
        if (options.ValidateSchema)
        {
            Console.WriteLine($"⏳ Validating schema for: {options.RulesFilePath}");
            var result = await validator.ValidateRulesFileAsync(options.RulesFilePath);

            if (result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Schema validation passed!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Schema validation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error}");
                }
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine("⚠️  Schema validation is disabled in configuration");
        }

        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static async Task DemoStructuredLogging(IServiceProvider serviceProvider)
    {
        Console.WriteLine("📝 Demo 2: Structured Logging");
        Console.WriteLine();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var engine = serviceProvider.GetRequiredService<IRuleEngine>();

        Console.WriteLine("Testing different log levels:");
        logger.LogDebug("This is a debug message");
        logger.LogInformation("This is an info message");
        logger.LogWarning("This is a warning message");
        logger.LogError("This is an error message", new InvalidOperationException("Test exception"));

        Console.WriteLine();
        Console.WriteLine("Running evaluation with configured logging:");
        
        var input = new ValidationInputDto();
        input.Properties["age"] = 25;
        input.Properties["country"] = "Argentina";
        input.Properties["email"] = "test@example.com";

        var report = await engine.EvaluateEnabledRulesAsync(input);
        Console.WriteLine($"Evaluation Status: [{report.Status}]");
        Console.WriteLine($"Passed: {report.TotalPassed}, Failed: {report.TotalFailed}");

        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoEnvironmentConfiguration(RuleEngineOptions options, string environment)
    {
        Console.WriteLine("🎭 Demo 3: Environment-Specific Configuration");
        Console.WriteLine();

        Console.WriteLine($"Current environment: {environment}");
        Console.WriteLine();
        Console.WriteLine("Configuration varies by environment:");
        Console.WriteLine();
        Console.WriteLine("Development:");
        Console.WriteLine("  - Debug logging enabled");
        Console.WriteLine("  - Structured log format");
        Console.WriteLine("  - Metrics enabled for debugging");
        Console.WriteLine();
        Console.WriteLine("Production:");
        Console.WriteLine("  - Warning level logging only");
        Console.WriteLine("  - JSON log format (for log aggregators)");
        Console.WriteLine("  - Longer cache expiration (30 min)");
        Console.WriteLine("  - Fail-fast mode (ContinueOnError = false)");
        Console.WriteLine();
        
        Console.WriteLine($"Active configuration:");
        Console.WriteLine($"  Log Level: {options.Logging.MinimumLevel}");
        Console.WriteLine($"  Log Format: {options.Logging.Format}");
        Console.WriteLine($"  Cache TTL: {options.Cache.ExpirationMinutes} min");
        Console.WriteLine();
    }

    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            _ => LogLevel.Information
        };
    }

    private static LogFormat ParseLogFormat(string format)
    {
        return format.ToLower() switch
        {
            "console" => LogFormat.Console,
            "structured" => LogFormat.Structured,
            "json" => LogFormat.Json,
            _ => LogFormat.Console
        };
    }
}
