using System.Text.Json;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Persistence.Mappers;
using RuleEngineCLI.Infrastructure.Persistence.Models;

namespace RuleEngineCLI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio de reglas que lee desde archivos JSON.
/// Aplica SOLID: DIP - implementa la abstracción definida en el dominio.
/// </summary>
public sealed class JsonRuleRepository : IRuleRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<Rule>? _cachedRules;

    public JsonRuleRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        _filePath = filePath;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    public async Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedRules != null)
            return _cachedRules;

        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"Rules file not found: {_filePath}");

        try
        {
            var jsonContent = await File.ReadAllTextAsync(_filePath, cancellationToken);
            
            var rulesFile = JsonSerializer.Deserialize<RulesFileModel>(jsonContent, _jsonOptions);
            
            if (rulesFile == null || rulesFile.Rules == null)
                throw new InvalidOperationException("Invalid rules file format. Expected 'rules' array.");

            _cachedRules = RuleMapper.ToDomainList(rulesFile.Rules).ToList();

            return _cachedRules;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse rules file: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load rules from file: {ex.Message}", ex);
        }
    }

    public async Task<Rule?> LoadRuleByIdAsync(RuleId ruleId, CancellationToken cancellationToken = default)
    {
        if (ruleId == null)
            throw new ArgumentNullException(nameof(ruleId));

        var rules = await LoadAllRulesAsync(cancellationToken);
        return rules.FirstOrDefault(r => r.Id == ruleId);
    }

    public async Task<IEnumerable<Rule>> LoadEnabledRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await LoadAllRulesAsync(cancellationToken);
        return rules.Where(r => r.IsEnabled).ToList();
    }

    /// <summary>
    /// Limpia la caché de reglas (útil para testing o recarga).
    /// </summary>
    public void ClearCache()
    {
        _cachedRules = null;
    }
}
