using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Presentation.CLI.DependencyInjection;
using RuleEngineCLI.Presentation.CLI.Utilities;

namespace RuleEngineCLI.Presentation.CLI;

/// <summary>
/// Punto de entrada de la aplicación.
/// Configura el CLI usando System.CommandLine para parsing robusto de argumentos.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configurar el comando raíz
        var rootCommand = new RootCommand("RuleEngineCLI - Configurable Business Rules Validation Engine");

        // Opciones
        var rulesFileOption = new Option<FileInfo>(
            aliases: new[] { "--rules", "-r" },
            description: "Path to the rules JSON file")
        {
            IsRequired = true
        };

        var inputFileOption = new Option<FileInfo?>(
            aliases: new[] { "--input", "-i" },
            description: "Path to the input data JSON file (optional if using inline data)");

        var inlineDataOption = new Option<string?>(
            aliases: new[] { "--data", "-d" },
            description: "Inline JSON data as a string (alternative to --input)");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output including all rules",
            getDefaultValue: () => false);

        var debugOption = new Option<bool>(
            aliases: new[] { "--debug" },
            description: "Enable debug logging",
            getDefaultValue: () => false);

        var onlyEnabledOption = new Option<bool>(
            aliases: new[] { "--only-enabled" },
            description: "Evaluate only enabled rules",
            getDefaultValue: () => true);

        rootCommand.AddOption(rulesFileOption);
        rootCommand.AddOption(inputFileOption);
        rootCommand.AddOption(inlineDataOption);
        rootCommand.AddOption(verboseOption);
        rootCommand.AddOption(debugOption);
        rootCommand.AddOption(onlyEnabledOption);

        // Handler
        rootCommand.SetHandler(async (context) =>
        {
            var rulesFile = context.ParseResult.GetValueForOption(rulesFileOption)!;
            var inputFile = context.ParseResult.GetValueForOption(inputFileOption);
            var inlineData = context.ParseResult.GetValueForOption(inlineDataOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var onlyEnabled = context.ParseResult.GetValueForOption(onlyEnabledOption);

            try
            {
                var exitCode = await ExecuteValidationAsync(
                    rulesFile.FullName,
                    inputFile?.FullName,
                    inlineData,
                    verbose,
                    debug,
                    onlyEnabled);

                context.ExitCode = exitCode;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
                Console.ResetColor();
                
                if (debug)
                {
                    Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
                }

                context.ExitCode = 99;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> ExecuteValidationAsync(
        string rulesFilePath,
        string? inputFilePath,
        string? inlineData,
        bool verbose,
        bool debug,
        bool onlyEnabled)
    {
        // Validar que existe al menos una fuente de datos
        if (string.IsNullOrEmpty(inputFilePath) && string.IsNullOrEmpty(inlineData))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[ERROR] You must provide either --input or --data option.");
            Console.WriteLine("Use --help for more information.");
            Console.ResetColor();
            return 99;
        }

        // Configurar logging level
        var logLevel = debug ? LogLevel.Debug : LogLevel.Information;

        // Configurar DI
        var serviceProvider = ServiceConfiguration.BuildServiceProvider(rulesFilePath, logLevel);
        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        // Banner
        PrintBanner();

        // Parsear input
        logger.LogInformation("Loading input data...");
        var inputDto = !string.IsNullOrEmpty(inputFilePath)
            ? await InputParser.ParseFromFileAsync(inputFilePath)
            : InputParser.ParseFromJson(inlineData!);

        logger.LogInformation($"Input data loaded: {inputDto.Properties.Count} properties");

        // Ejecutar validación
        logger.LogInformation($"Starting validation (only enabled: {onlyEnabled})...");
        
        var report = onlyEnabled
            ? await ruleEngine.EvaluateEnabledRulesAsync(inputDto)
            : await ruleEngine.EvaluateAsync(inputDto);

        // Mostrar reporte
        ReportFormatter.PrintReport(report, verbose);

        // Retornar código de salida basado en el resultado
        return ReportFormatter.GetExitCode(report);
    }

    private static void PrintBanner()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║              RULE ENGINE CLI v1.0                             ║
║              Configurable Business Rules Validator            ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
");
        Console.ResetColor();
    }
}
