using RuleEngineCLI.Application.DTOs;

namespace RuleEngineCLI.Presentation.CLI.Utilities;

/// <summary>
/// Formateador de reportes de validación para consola.
/// Genera output legible y profesional.
/// </summary>
public static class ReportFormatter
{
    public static void PrintReport(ValidationReportDto report, bool verbose = false)
    {
        Console.WriteLine();
        PrintSeparator('=');
        PrintCentered("RULE ENGINE VALIDATION REPORT");
        PrintSeparator('=');
        Console.WriteLine();

        // Información general
        Console.WriteLine($"Generated At:        {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Total Rules:         {report.TotalRulesEvaluated}");
        Console.WriteLine($"Rules Passed:        {report.TotalPassed}");
        Console.WriteLine($"Rules Failed:        {report.TotalFailed}");
        Console.WriteLine($"Max Severity Found:  {report.MaxSeverity}");
        Console.WriteLine();

        // Estado final con color
        var statusColor = report.Status switch
        {
            "PASS" => ConsoleColor.Green,
            "WARNING" => ConsoleColor.Yellow,
            "FAIL" => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        Console.Write("Final Status:        ");
        PrintColored($"[{report.Status}]", statusColor, newLine: true);
        Console.WriteLine();

        // Detalles de reglas fallidas
        var failedResults = report.Results.Where(r => !r.Passed).ToList();
        
        if (failedResults.Any())
        {
            PrintSeparator('-');
            PrintColored("FAILED RULES", ConsoleColor.Red, newLine: true);
            PrintSeparator('-');
            Console.WriteLine();

            foreach (var result in failedResults)
            {
                var severityColor = result.Severity switch
                {
                    "INFO" => ConsoleColor.Cyan,
                    "WARN" or "WARNING" => ConsoleColor.Yellow,
                    "ERROR" => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.Write($"  [{result.RuleId}] ");
                PrintColored($"[{result.Severity}]", severityColor, newLine: false);
                Console.WriteLine();
                Console.WriteLine($"    Description: {result.Description}");
                Console.WriteLine($"    Message:     {result.Message}");
                Console.WriteLine();
            }
        }

        // Modo verbose: mostrar todas las reglas
        if (verbose)
        {
            PrintSeparator('-');
            Console.WriteLine("ALL RULES (DETAILED)");
            PrintSeparator('-');
            Console.WriteLine();

            foreach (var result in report.Results)
            {
                var statusIcon = result.Passed ? "✓" : "✗";
                var statusColor = result.Passed ? ConsoleColor.Green : ConsoleColor.Red;

                Console.Write("  ");
                PrintColored(statusIcon, statusColor, newLine: false);
                Console.WriteLine($" [{result.RuleId}] {result.Description}");
                
                if (!result.Passed)
                    Console.WriteLine($"    → {result.Message}");
            }
            Console.WriteLine();
        }

        PrintSeparator('=');
        Console.WriteLine();
    }

    public static int GetExitCode(ValidationReportDto report)
    {
        return report.Status switch
        {
            "PASS" => 0,
            "WARNING" => 1,
            "FAIL" => 2,
            _ => 99
        };
    }

    private static void PrintSeparator(char character, int length = 70)
    {
        Console.WriteLine(new string(character, length));
    }

    private static void PrintCentered(string text, int width = 70)
    {
        var padding = (width - text.Length) / 2;
        Console.WriteLine(new string(' ', Math.Max(0, padding)) + text);
    }

    private static void PrintColored(string text, ConsoleColor color, bool newLine = false)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            if (newLine)
                Console.WriteLine(text);
            else
                Console.Write(text);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
