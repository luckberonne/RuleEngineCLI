using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

namespace RuleEngineCLI.ConsumerExample;

/// <summary>
/// Ejemplo de cómo consumir RuleEngineCLI como una librería desde otro proyecto .NET
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== RuleEngineCLI - Consumer Example ===\n");

        // Configurar el contenedor de Dependency Injection
        var serviceProvider = ConfigureServices();

        // Obtener el servicio IRuleEngine
        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

        // Ejemplo 1: Usuario válido
        Console.WriteLine("📋 Ejemplo 1: Validando usuario válido...\n");
        await ValidateUser(ruleEngine, new UserData
        {
            Age = 25,
            Balance = 1500.50m,
            Username = "john.doe",
            StartDate = DateTime.Parse("2026-01-01"),
            EndDate = DateTime.Parse("2026-12-31"),
            EmailDomain = "company.com",
            TransactionAmount = 5000,
            IsActive = true,
            IsVerified = true
        });

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // Ejemplo 2: Usuario inválido
        Console.WriteLine("📋 Ejemplo 2: Validando usuario inválido...\n");
        await ValidateUser(ruleEngine, new UserData
        {
            Age = 16,  // Menor de 18
            Balance = -100,  // Balance negativo
            Username = null,  // Username nulo
            StartDate = DateTime.Parse("2026-12-31"),
            EndDate = DateTime.Parse("2026-01-01"),  // Fechas invertidas
            EmailDomain = "gmail.com",
            TransactionAmount = 15000,
            IsActive = true,
            IsVerified = false  // No verificado
        });

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // Ejemplo 3: Validación programática con reglas en memoria
        Console.WriteLine("📋 Ejemplo 3: Validación programática personalizada...\n");
        await CustomValidationExample();
    }

    /// <summary>
    /// Configura los servicios de RuleEngineCLI usando Dependency Injection
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configurar logger (usa ConsoleLogger de la infraestructura)
        services.AddSingleton<ILogger>(sp => 
            new ConsoleLogger(LogLevel.Information, includeTimestamp: false));

        // Configurar repositorio de reglas apuntando al archivo JSON
        var rulesPath = Path.Combine("..", "rules.json");
        services.AddSingleton<IRuleRepository>(sp => 
            new JsonRuleRepository(rulesPath));

        // Configurar evaluador de expresiones
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();

        // Configurar el motor de reglas
        services.AddSingleton<IRuleEngine>(sp =>
        {
            var repo = sp.GetRequiredService<IRuleRepository>();
            var evaluator = sp.GetRequiredService<IExpressionEvaluator>();
            var logger = sp.GetRequiredService<ILogger>();
            return new RuleEngineCLI.Application.Implementation.RuleEngine(repo, evaluator, logger);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Valida los datos de un usuario usando el motor de reglas
    /// </summary>
    private static async Task ValidateUser(IRuleEngine ruleEngine, UserData user)
    {
        // Convertir el objeto UserData a ValidationInputDto
        var inputData = new ValidationInputDto(new Dictionary<string, object?>
        {
            { "age", user.Age },
            { "balance", user.Balance },
            { "username", user.Username },
            { "startDate", user.StartDate?.ToString("yyyy-MM-dd") },
            { "endDate", user.EndDate?.ToString("yyyy-MM-dd") },
            { "emailDomain", user.EmailDomain },
            { "transactionAmount", user.TransactionAmount },
            { "isActive", user.IsActive },
            { "isVerified", user.IsVerified }
        });

        // Ejecutar la validación
        var report = await ruleEngine.EvaluateEnabledRulesAsync(inputData);

        // Mostrar resultados
        Console.WriteLine($"Estado de validación: {GetStatusWithColor(report.Status)}");
        Console.WriteLine($"Total de reglas evaluadas: {report.TotalRulesEvaluated}");
        Console.WriteLine($"Reglas que pasaron: {report.TotalPassed}");
        Console.WriteLine($"Reglas que fallaron: {report.TotalFailed}");
        Console.WriteLine($"Severidad máxima: {report.MaxSeverity}");

        // Si hay errores, mostrarlos
        if (report.TotalFailed > 0)
        {
            Console.WriteLine("\n❌ Errores encontrados:");
            foreach (var result in report.Results.Where(r => !r.Passed))
            {
                Console.ForegroundColor = result.Severity == "ERROR" 
                    ? ConsoleColor.Red 
                    : ConsoleColor.Yellow;
                Console.WriteLine($"  [{result.RuleId}] {result.Message}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.WriteLine("\n✅ Todos los datos son válidos!");
        }
    }

    /// <summary>
    /// Ejemplo de validación personalizada sin archivo JSON
    /// </summary>
    private static async Task CustomValidationExample()
    {
        Console.WriteLine("Este ejemplo podría validar datos dinámicamente sin archivo JSON.");
        Console.WriteLine("Para implementarlo, necesitarías crear un repositorio en memoria.");
        Console.WriteLine("Por ahora, este proyecto usa el archivo rules.json existente.");
    }

    /// <summary>
    /// Obtiene el texto del estado con color
    /// </summary>
    private static string GetStatusWithColor(string status)
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

/// <summary>
/// Clase que representa los datos de un usuario a validar
/// </summary>
public class UserData
{
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public string? Username { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EmailDomain { get; set; }
    public decimal TransactionAmount { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
}
