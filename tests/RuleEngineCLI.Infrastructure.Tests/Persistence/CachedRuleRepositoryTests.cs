using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;
using Xunit;

namespace RuleEngineCLI.Infrastructure.Tests.Persistence;

public class CachedRuleRepositoryTests
{
    private sealed class CountingRuleRepository : IRuleRepository
    {
        public int LoadAllCalls { get; private set; }
        public int LoadEnabledCalls { get; private set; }
        private readonly Rule _rule = Rule.Create(
            RuleId.Create("RULE_001"),
            "Test rule",
            Expression.Create("age > 18"),
            Severity.Error,
            "Test error");

        public Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken cancellationToken = default)
        {
            LoadAllCalls++;
            return Task.FromResult<IEnumerable<Rule>>(new[] { _rule });
        }

        public Task<Rule?> LoadRuleByIdAsync(RuleId ruleId, CancellationToken cancellationToken = default)
            => Task.FromResult<Rule?>(_rule);

        public Task<IEnumerable<Rule>> LoadEnabledRulesAsync(CancellationToken cancellationToken = default)
        {
            LoadEnabledCalls++;
            return Task.FromResult<IEnumerable<Rule>>(new[] { _rule });
        }
    }

    [Fact]
    public async Task LoadAllRulesAsync_CalledTwice_OnlyHitsInnerRepositoryOnce()
    {
        var inner = new CountingRuleRepository();
        var repository = new CachedRuleRepository(inner, new MemoryCache(new MemoryCacheOptions()));

        await repository.LoadAllRulesAsync();
        await repository.LoadAllRulesAsync();

        inner.LoadAllCalls.Should().Be(1);
    }

    [Fact]
    public async Task LoadEnabledRulesAsync_CachedIndependentlyFromLoadAll()
    {
        var inner = new CountingRuleRepository();
        var repository = new CachedRuleRepository(inner, new MemoryCache(new MemoryCacheOptions()));

        await repository.LoadAllRulesAsync();
        await repository.LoadEnabledRulesAsync();

        inner.LoadAllCalls.Should().Be(1);
        inner.LoadEnabledCalls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateCache_ForcesReloadFromInnerRepository()
    {
        var inner = new CountingRuleRepository();
        var repository = new CachedRuleRepository(inner, new MemoryCache(new MemoryCacheOptions()));

        await repository.LoadAllRulesAsync();
        repository.InvalidateCache();
        await repository.LoadAllRulesAsync();

        inner.LoadAllCalls.Should().Be(2);
    }
}
