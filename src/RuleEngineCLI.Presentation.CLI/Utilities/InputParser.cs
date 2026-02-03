using System.Text.Json;
using RuleEngineCLI.Application.DTOs;

namespace RuleEngineCLI.Presentation.CLI.Utilities;

/// <summary>
/// Utilidad para parsear diferentes formatos de entrada de datos.
/// </summary>
public static class InputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Parsea un archivo JSON a ValidationInputDto.
    /// </summary>
    public static async Task<ValidationInputDto> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Input file not found: {filePath}");

        var jsonContent = await File.ReadAllTextAsync(filePath);
        return ParseFromJson(jsonContent);
    }

    /// <summary>
    /// Parsea una cadena JSON a ValidationInputDto.
    /// </summary>
    public static ValidationInputDto ParseFromJson(string json)
    {
        try
        {
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            
            if (dictionary == null)
                throw new InvalidOperationException("Invalid JSON format.");

            // Convertir JsonElement a tipos apropiados
            var normalizedDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var kvp in dictionary)
            {
                normalizedDict[kvp.Key] = NormalizeJsonElement(kvp.Value);
            }

            return new ValidationInputDto(normalizedDict);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON input: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parsea argumentos de línea de comandos en formato key=value.
    /// Ejemplo: name=John age=30 isActive=true
    /// </summary>
    public static ValidationInputDto ParseFromCommandLineArgs(string[] args)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            var parts = arg.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            properties[key] = ParseValue(value);
        }

        return new ValidationInputDto(properties);
    }

    private static object? NormalizeJsonElement(object? value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) 
                    ? intVal 
                    : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => jsonElement.ToString()
            };
        }

        return value;
    }

    private static object? ParseValue(string value)
    {
        // Intentar parsear como null
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        // Intentar parsear como boolean
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        // Intentar parsear como número entero
        if (int.TryParse(value, out var intValue))
            return intValue;

        // Intentar parsear como número decimal
        if (double.TryParse(value, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
            return doubleValue;

        // Intentar parsear como fecha
        if (DateTime.TryParse(value, out var dateValue))
            return dateValue.ToString("yyyy-MM-dd");

        // Por defecto, retornar como cadena (sin comillas)
        return value.Trim('"', '\'');
    }
}
