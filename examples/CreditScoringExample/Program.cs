using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

namespace RuleEngineCLI.CreditScoringExample;

/// <summary>
/// Ejemplo completo de Scoring de Riesgo Crediticio usando RuleEngineCLI
/// Demuestra cómo integrar reglas de negocio complejas en una aplicación financiera
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏦 RuleEngineCLI - Credit Risk Scoring Example");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        // Configurar servicios de inyección de dependencias
        var serviceProvider = ConfigureServices();

        // Ejecutar diferentes escenarios de scoring
        await RunCreditScoringScenarios(serviceProvider);
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configurar repositorio de reglas (desde archivo JSON)
        services.AddSingleton<IRuleRepository>(sp =>
            new JsonRuleRepository("c:/RuleEngineCLI/examples/credit-scoring-rules.json"));

        // Usar evaluador de comparación para expresiones simples
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();

        // Configurar logging
        services.AddSingleton<ILogger, ConsoleLogger>();

        // Registrar el motor de reglas principal
        services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();

        return services.BuildServiceProvider();
    }

    private static async Task RunCreditScoringScenarios(IServiceProvider services)
    {
        var ruleEngine = services.GetRequiredService<IRuleEngine>();

        // Escenario 1: Solicitante de Bajo Riesgo (Excelente)
        Console.WriteLine("📊 Escenario 1: Solicitante de BAJO RIESGO");
        Console.WriteLine("─────────────────────────────────────────");
        var lowRiskApplicant = await LoadApplicantData("c:/RuleEngineCLI/examples/credit-applicant-good.json");
        var lowRiskResult = await ruleEngine.EvaluateAsync(lowRiskApplicant);
        DisplayCreditScoringResult(lowRiskResult, "BAJO RIESGO");
        Console.WriteLine();

        // Escenario 2: Solicitante de Riesgo Moderado
        Console.WriteLine("📊 Escenario 2: Solicitante de RIESGO MODERADO");
        Console.WriteLine("────────────────────────────────────────────");
        var moderateRiskApplicant = await LoadApplicantData("c:/RuleEngineCLI/examples/credit-applicant-moderate.json");
        var moderateRiskResult = await ruleEngine.EvaluateAsync(moderateRiskApplicant);
        DisplayCreditScoringResult(moderateRiskResult, "RIESGO MODERADO");
        Console.WriteLine();

        // Escenario 3: Solicitante de Alto Riesgo
        Console.WriteLine("📊 Escenario 3: Solicitante de ALTO RIESGO");
        Console.WriteLine("─────────────────────────────────────────");
        var highRiskApplicant = await LoadApplicantData("c:/RuleEngineCLI/examples/credit-applicant-high-risk.json");
        var highRiskResult = await ruleEngine.EvaluateAsync(highRiskApplicant);
        DisplayCreditScoringResult(highRiskResult, "ALTO RIESGO");
        Console.WriteLine();

        // Mostrar resumen comparativo
        DisplayScoringSummary(lowRiskResult, moderateRiskResult, highRiskResult);
    }

    private static async Task<ValidationInputDto> LoadApplicantData(string filePath)
    {
        // En una aplicación real, estos datos vendrían de:
        // - API REST (datos del solicitante)
        // - Base de datos (historial crediticio)
        // - Servicios externos (buró de crédito)
        // - Formularios web (datos del usuario)

        var jsonContent = await File.ReadAllTextAsync(filePath);
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonContent);

        return new ValidationInputDto(data ?? new Dictionary<string, object?>());
    }

    private static void DisplayCreditScoringResult(ValidationReportDto result, string riskCategory)
    {
        Console.WriteLine($"Estado General: {result.Status}");
        Console.WriteLine($"Reglas Evaluadas: {result.TotalRulesEvaluated}");
        Console.WriteLine($"Reglas Exitosas: {result.TotalPassed}");
        Console.WriteLine($"Reglas Fallidas: {result.TotalFailed}");
        Console.WriteLine($"Severidad Máxima: {result.MaxSeverity}");

        var failedRules = result.Results.Where(r => !r.Passed).ToList();
        if (failedRules.Any())
        {
            Console.WriteLine("\n❌ Problemas Identificados:");
            foreach (var failure in failedRules)
            {
                Console.WriteLine($"  • {failure.RuleId}: {failure.Message}");
            }
        }
        else
        {
            Console.WriteLine("\n✅ Todas las reglas pasaron - Candidato aprobado");
        }

        // Calcular score basado en reglas
        var score = CalculateCreditScore(result);
        Console.WriteLine($"\n🎯 Puntaje Crediticio Calculado: {score}/100");
        Console.WriteLine($"📈 Categoría de Riesgo: {riskCategory}");
    }

    private static int CalculateCreditScore(ValidationReportDto result)
    {
        // Sistema de scoring simple basado en reglas
        // En producción, esto sería más sofisticado con algoritmos de ML

        int baseScore = 100;

        // Penalizaciones por severidad
        var failedRules = result.Results.Where(r => !r.Passed).ToList();
        foreach (var failure in failedRules)
        {
            switch (failure.Severity)
            {
                case "ERROR":
                    baseScore -= 25; // Penalización alta
                    break;
                case "WARN":
                    baseScore -= 10; // Penalización media
                    break;
                case "INFO":
                    baseScore -= 5;  // Penalización baja
                    break;
            }
        }

        // Bonus por reglas pasadas
        baseScore += result.TotalPassed * 2;

        // Asegurar rango válido
        return Math.Max(0, Math.Min(100, baseScore));
    }

    private static void DisplayScoringSummary(
        ValidationReportDto lowRisk,
        ValidationReportDto moderateRisk,
        ValidationReportDto highRisk)
    {
        Console.WriteLine("📈 RESUMEN COMPARATIVO DE SCORING CREDITICIO");
        Console.WriteLine("══════════════════════════════════════════════════════════");

        var scenarios = new[]
        {
            ("BAJO RIESGO", lowRisk, CalculateCreditScore(lowRisk)),
            ("MODERADO", moderateRisk, CalculateCreditScore(moderateRisk)),
            ("ALTO RIESGO", highRisk, CalculateCreditScore(highRisk))
        };

        Console.WriteLine("│ Escenario      │ Estado    │ Puntaje │ Reglas │ Errores │");
        Console.WriteLine("│────────────────│───────────│─────────│────────│─────────│");

        foreach (var (name, result, score) in scenarios)
        {
            Console.WriteLine($"│ {name,-14} │ {result.Status,-9} │ {score,3}/100 │ {result.TotalPassed,2}/{result.TotalRulesEvaluated,-2} │ {result.TotalFailed,2}       │");
        }

        Console.WriteLine("══════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("💡 Interpretación de Resultados:");
        Console.WriteLine("   • 80-100: Excelente candidato - Aprobación automática");
        Console.WriteLine("   • 60-79:  Buen candidato - Revisión adicional mínima");
        Console.WriteLine("   • 40-59:  Candidato riesgoso - Revisión manual requerida");
        Console.WriteLine("   • 0-39:   Alto riesgo - Probablemente rechazar");
    }
}