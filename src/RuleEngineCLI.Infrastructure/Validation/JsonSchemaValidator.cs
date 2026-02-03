using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RuleEngineCLI.Infrastructure.Validation;

/// <summary>
/// Validador de esquema JSON para archivos de reglas.
/// </summary>
public class JsonSchemaValidator
{
    /// <summary>
    /// Valida que un archivo JSON cumpla con el esquema de reglas.
    /// </summary>
    public async Task<ValidationResult> ValidateRulesFileAsync(string filePath)
    {
        var errors = new List<string>();

        // Verificar que el archivo existe
        if (!File.Exists(filePath))
        {
            errors.Add($"File not found: {filePath}");
            return new ValidationResult(false, errors);
        }

        try
        {
            // Leer y parsear el JSON
            var jsonContent = await File.ReadAllTextAsync(filePath);
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Validar estructura raíz
            if (!root.TryGetProperty("rules", out var rulesArray))
            {
                errors.Add("Missing required property 'rules' at root level");
                return new ValidationResult(false, errors);
            }

            if (rulesArray.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Property 'rules' must be an array");
                return new ValidationResult(false, errors);
            }

            // Validar cada regla
            var ruleIndex = 0;
            foreach (var rule in rulesArray.EnumerateArray())
            {
                ValidateRule(rule, ruleIndex, errors);
                ruleIndex++;
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
            return new ValidationResult(false, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            return new ValidationResult(false, errors);
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    private void ValidateRule(JsonElement rule, int index, List<string> errors)
    {
        var prefix = $"Rule[{index}]";

        // Validar propiedades requeridas
        ValidateRequiredProperty(rule, "id", prefix, errors);
        ValidateRequiredProperty(rule, "name", prefix, errors);
        ValidateRequiredProperty(rule, "expression", prefix, errors);
        ValidateRequiredProperty(rule, "severity", prefix, errors);

        // Validar tipos
        if (rule.TryGetProperty("id", out var id) && id.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{prefix}.id must be a string");
        }

        if (rule.TryGetProperty("name", out var name) && name.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{prefix}.name must be a string");
        }

        if (rule.TryGetProperty("expression", out var expression) && expression.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{prefix}.expression must be a string");
        }

        if (rule.TryGetProperty("severity", out var severity))
        {
            if (severity.ValueKind != JsonValueKind.String)
            {
                errors.Add($"{prefix}.severity must be a string");
            }
            else
            {
                var severityValue = severity.GetString();
                if (severityValue != "Error" && severityValue != "Warning" && severityValue != "Information")
                {
                    errors.Add($"{prefix}.severity must be 'Error', 'Warning', or 'Information'");
                }
            }
        }

        // Validar enabled (opcional)
        if (rule.TryGetProperty("enabled", out var enabled) && enabled.ValueKind != JsonValueKind.True && enabled.ValueKind != JsonValueKind.False)
        {
            errors.Add($"{prefix}.enabled must be a boolean");
        }

        // Validar message (opcional)
        if (rule.TryGetProperty("message", out var message) && message.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{prefix}.message must be a string");
        }
    }

    private void ValidateRequiredProperty(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!element.TryGetProperty(propertyName, out _))
        {
            errors.Add($"{prefix} is missing required property '{propertyName}'");
        }
    }
}

/// <summary>
/// Resultado de validación de esquema.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors.AsReadOnly();
    }
}
