using Microsoft.Extensions.Caching.Memory;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;

namespace RuleEngineCLI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio que implementa caché en memoria para mejorar el rendimiento.
/// Envuelve otro repositorio y cachea los resultados.
/// </summary>
public sealed class CachedRuleRepository : IRuleRepository
{
    private readonly IRuleRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;
    
    private const string AllRulesCacheKey = "all-rules";
    private const string EnabledRulesCacheKey = "enabled-rules";

    public CachedRuleRepository(
        IRuleRepository innerRepository,
        IMemoryCache cache,
        TimeSpan? cacheDuration = null)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(AllRulesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return await _innerRepository.LoadAllRulesAsync(cancellationToken);
        }) ?? Enumerable.Empty<Rule>();
    }

    public async Task<Rule?> LoadRuleByIdAsync(RuleId ruleId, CancellationToken cancellationToken = default)
    {
        if (ruleId == null)
            throw new ArgumentNullException(nameof(ruleId));

        var cacheKey = $"rule-{ruleId.Value}";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return await _innerRepository.LoadRuleByIdAsync(ruleId, cancellationToken);
        });
    }

    public async Task<IEnumerable<Rule>> LoadEnabledRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(EnabledRulesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return await _innerRepository.LoadEnabledRulesAsync(cancellationToken);
        }) ?? Enumerable.Empty<Rule>();
    }

    /// <summary>
    /// Invalida toda la caché de reglas.
    /// Útil cuando se actualizan las reglas externamente.
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Remove(AllRulesCacheKey);
        _cache.Remove(EnabledRulesCacheKey);
    }

    /// <summary>
    /// Invalida la caché de una regla específica.
    /// </summary>
    public void InvalidateCache(RuleId ruleId)
    {
        if (ruleId == null)
            throw new ArgumentNullException(nameof(ruleId));
            
        _cache.Remove($"rule-{ruleId.Value}");
        _cache.Remove(AllRulesCacheKey);
        _cache.Remove(EnabledRulesCacheKey);
    }
}
