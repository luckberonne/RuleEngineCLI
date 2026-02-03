using System.Text.Json.Serialization;

namespace RuleEngineCLI.Infrastructure.Persistence.Models;

/// <summary>
/// Modelo de datos para serialización/deserialización de reglas en JSON.
/// Aplica patrón DTO para separar el modelo de persistencia del modelo de dominio.
/// </summary>
public sealed class RuleJsonModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Contenedor para el archivo de reglas JSON.
/// </summary>
public sealed class RulesFileModel
{
    [JsonPropertyName("rules")]
    public List<RuleJsonModel> Rules { get; set; } = new();

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
