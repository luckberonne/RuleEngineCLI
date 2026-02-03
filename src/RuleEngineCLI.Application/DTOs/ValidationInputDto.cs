namespace RuleEngineCLI.Application.DTOs;

/// <summary>
/// DTO para entrada de datos a validar.
/// Representa un diccionario flexible de propiedades clave-valor.
/// </summary>
public sealed class ValidationInputDto
{
    public Dictionary<string, object?> Properties { get; init; }

    public ValidationInputDto()
    {
        Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public ValidationInputDto(Dictionary<string, object?> properties)
    {
        Properties = new Dictionary<string, object?>(properties, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Obtiene un valor tipado de forma segura.
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
            return default;

        if (value == null)
            return default;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Verifica si existe una propiedad.
    /// </summary>
    public bool HasProperty(string key) => Properties.ContainsKey(key);
}
