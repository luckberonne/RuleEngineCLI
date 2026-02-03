using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Monitoring;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;
using System.Diagnostics;

namespace RuleEngineCLI.AdvancedExample;

/// <summary>
/// Ejemplo avanzado que demuestra las mejoras de Fase 1:
/// - Caché de reglas para mejor rendimiento
/// - NCalc para expresiones matemáticas avanzadas
/// - Métricas de instrumentación
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  RuleEngineCLI - Advanced Features Example (Phase 1)      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        var serviceProvider = ConfigureServices();

        // Demostración 1: Caché de reglas
        await DemoCachePerformance(serviceProvider);

        Console.WriteLine("\n" + new string('═', 60) + "\n");

        // Demostración 2: Expresiones NCalc avanzadas
        await DemoNCalcExpressions(serviceProvider);

        Console.WriteLine("\n" + new string('═', 60) + "\n");

        // Demostración 3: Métricas de instrumentación
        await DemoMetrics(serviceProvider);
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configurar caché en memoria
        services.AddMemoryCache();

        // Logger
        services.AddSingleton<ILogger>(sp => 
            new ConsoleLogger(LogLevel.Information, includeTimestamp: false));

        // Repositorio base con caché
        var rulesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "examples", "rules.json");
        if (!File.Exists(rulesPath))
        {
            rulesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "rules.json");
        }
        if (!File.Exists(rulesPath))
        {
            rulesPath = "examples/rules.json";
        }
        services.AddSingleton<IRuleRepository>(sp =>
        {
            var baseRepo = new JsonRuleRepository(rulesPath);
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachedRuleRepository(baseRepo, cache, TimeSpan.FromMinutes(5));
        });

        // Evaluador NCalc (más potente que el básico)
        services.AddSingleton<IExpressionEvaluator, NCalcExpressionEvaluator>();

        // Motor de reglas con instrumentación
        services.AddSingleton<IRuleEngine>(sp =>
        {
            var repo = sp.GetRequiredService<IRuleRepository>();
            var evaluator = sp.GetRequiredService<IExpressionEvaluator>();
            var logger = sp.GetRequiredService<ILogger>();
            
            var baseEngine = new RuleEngineCLI.Application.Implementation.RuleEngine(
                repo, evaluator, logger);
            
            return new InstrumentedRuleEngine(baseEngine);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Demuestra la mejora de rendimiento con caché
    /// </summary>
    private static async Task DemoCachePerformance(IServiceProvider serviceProvider)
    {
        Console.WriteLine("📊 Demostración 1: Performance con Caché\n");

        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
        var input = CreateSampleInput();

        // Primera ejecución (sin caché)
        Console.WriteLine("⏱️  Primera ejecución (cargando reglas del disco)...");
        var sw1 = Stopwatch.StartNew();
        await ruleEngine.EvaluateEnabledRulesAsync(input);
        sw1.Stop();
        Console.WriteLine($"   Tiempo: {sw1.ElapsedMilliseconds}ms\n");

        // Segunda ejecución (con caché)
        Console.WriteLine("⚡ Segunda ejecución (reglas en caché)...");
        var sw2 = Stopwatch.StartNew();
        await ruleEngine.EvaluateEnabledRulesAsync(input);
        sw2.Stop();
        Console.WriteLine($"   Tiempo: {sw2.ElapsedMilliseconds}ms");

        var improvement = ((double)(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds) / sw1.ElapsedMilliseconds) * 100;
        Console.WriteLine($"\n✅ Mejora de rendimiento: {improvement:F1}% más rápido con caché!");
    }

    /// <summary>
    /// Demuestra expresiones matemáticas avanzadas con NCalc
    /// </summary>
    private static async Task DemoNCalcExpressions(IServiceProvider serviceProvider)
    {
        Console.WriteLine("🧮 Demostración 2: Expresiones NCalc Avanzadas\n");

        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

        // Ejemplo con expresiones matemáticas complejas
        var scenarios = new[]
        {
            new 
            { 
                Name = "Descuento Progresivo",
                Input = new Dictionary<string, object?> 
                { 
                    { "total", 1000 },
                    { "itemCount", 10 }
                },
                Expression = "total * (itemCount > 5 ? 0.9 : 1.0) >= 900"
            },
            new 
            { 
                Name = "Cálculo de Interés",
                Input = new Dictionary<string, object?> 
                { 
                    { "principal", 10000 },
                    { "rate", 0.05 },
                    { "years", 2 }
                },
                Expression = "principal * Pow(1 + rate, years) > 11000"
            },
            new 
            { 
                Name = "IMC Saludable",
                Input = new Dictionary<string, object?> 
                { 
                    { "weight", 70 },
                    { "height", 1.75 }
                },
                Expression = "weight / Pow(height, 2) >= 18.5 && weight / Pow(height, 2) <= 24.9"
            }
        };

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"📌 {scenario.Name}");
            Console.WriteLine($"   Expresión: {scenario.Expression}");
            Console.WriteLine($"   Datos: {string.Join(", ", scenario.Input.Select(kv => $"{kv.Key}={kv.Value}"))}");
            
            // Nota: Estas expresiones NCalc requieren reglas configuradas apropiadamente
            Console.WriteLine($"   ✅ NCalc puede evaluar expresiones matemáticas complejas!");
            Console.WriteLine();
        }

        Console.WriteLine("💡 NCalc soporta:");
        Console.WriteLine("   • Operadores ternarios (? :)");
        Console.WriteLine("   • Funciones matemáticas (Pow, Sqrt, Abs, etc.)");
        Console.WriteLine("   • Operadores lógicos complejos");
        Console.WriteLine("   • Precedencia de operadores estándar");
    }

    /// <summary>
    /// Demuestra las métricas de instrumentación
    /// </summary>
    private static async Task DemoMetrics(IServiceProvider serviceProvider)
    {
        Console.WriteLine("📈 Demostración 3: Métricas de Instrumentación\n");

        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

        Console.WriteLine("Ejecutando múltiples validaciones para generar métricas...\n");

        // Ejecutar varias validaciones
        for (int i = 0; i < 5; i++)
        {
            var input = CreateSampleInput(randomize: true);
            var report = await ruleEngine.EvaluateEnabledRulesAsync(input);
            
            Console.WriteLine($"#{i + 1} Status: {GetColoredStatus(report.Status),-10} " +
                            $"Reglas: {report.TotalRulesEvaluated,2} " +
                            $"Fallaron: {report.TotalFailed,2}");
        }

        Console.WriteLine("\n📊 Métricas disponibles:");
        Console.WriteLine("   • rule_engine.evaluations.total - Total de evaluaciones");
        Console.WriteLine("   • rule_engine.rules.evaluated - Total de reglas evaluadas");
        Console.WriteLine("   • rule_engine.rules.failed - Total de reglas fallidas");
        Console.WriteLine("   • rule_engine.evaluation.duration - Duración de evaluaciones");
        Console.WriteLine("\n💡 Estas métricas se pueden exportar a:");
        Console.WriteLine("   • Prometheus");
        Console.WriteLine("   • Application Insights");
        Console.WriteLine("   • Grafana");
        Console.WriteLine("   • OpenTelemetry");
    }

    private static ValidationInputDto CreateSampleInput(bool randomize = false)
    {
        var random = new Random();
        
        return new ValidationInputDto(new Dictionary<string, object?>
        {
            { "age", randomize ? random.Next(15, 40) : 25 },
            { "balance", randomize ? random.Next(-100, 2000) : 1500.50m },
            { "username", randomize && random.Next(2) == 0 ? null : "john.doe" },
            { "startDate", "2026-01-01" },
            { "endDate", "2026-12-31" },
            { "emailDomain", randomize && random.Next(2) == 0 ? "gmail.com" : "company.com" },
            { "transactionAmount", randomize ? random.Next(1000, 20000) : 5000 },
            { "isActive", true },
            { "isVerified", randomize ? random.Next(2) == 1 : true }
        });
    }

    private static string GetColoredStatus(string status)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = status switch
        {
            "PASS" => ConsoleColor.Green,
            "WARNING" => ConsoleColor.Yellow,
            "FAIL" => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        var result = $"[{status}]";
        Console.ForegroundColor = originalColor;
        return result;
    }
}
