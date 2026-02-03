using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Implementation;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Entities;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Domain.ValueObjects;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Performance;

namespace RuleEngineCLI.PerformanceExample;

/// <summary>
/// Ejemplo de benchmarks comparando diferentes implementaciones (Phase 4).
/// Demuestra mejoras de performance con:
/// - ParallelRuleEngine vs RuleEngine estándar
/// - CompiledExpressionEvaluator vs ComparisonExpressionEvaluator
/// - ObjectPool vs allocations directas
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  RuleEngineCLI - Performance Benchmarks (Phase 4)         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Demo rápido antes de los benchmarks completos
        Console.WriteLine("🚀 Quick Performance Demo");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        DemoParallelPerformance();
        Console.WriteLine();

        DemoCompiledExpressions();
        Console.WriteLine();

        DemoObjectPool();
        Console.WriteLine();

        // Ejecutar benchmarks completos (comentar si solo quieres ver el demo)
        Console.WriteLine("📊 Running Full Benchmarks (this may take several minutes)...");
        Console.WriteLine("Press Ctrl+C to skip benchmarks, or Enter to continue.");
        Console.WriteLine();

        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            var summary = BenchmarkRunner.Run<RuleEngineBenchmarks>();
        }
    }

    private static void DemoParallelPerformance()
    {
        Console.WriteLine("⚡ Demo 1: Parallel Rule Engine");
        Console.WriteLine();

        var logger = new ConsoleLogger();
        var rules = CreateTestRules(50); // 50 reglas para evaluar
        var input = CreateTestInput();

        // Simular repositorio en memoria
        var repository = new InMemoryRuleRepository(rules);
        var evaluators = new IExpressionEvaluator[]
        {
            new ComparisonExpressionEvaluator()
        };

        // RuleEngine estándar (secuencial)
        var standardEngine = new RuleEngine(repository, evaluators.First(), logger);

        // ParallelRuleEngine (concurrente)
        var parallelEngine = new ParallelRuleEngine(repository, evaluators, logger);

        // Benchmark
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result1 = standardEngine.EvaluateEnabledRulesAsync(input).Result;
        sw.Stop();
        var sequentialTime = sw.ElapsedMilliseconds;

        sw.Restart();
        var result2 = parallelEngine.EvaluateEnabledRulesAsync(input).Result;
        sw.Stop();
        var parallelTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Sequential Engine: {sequentialTime}ms");
        Console.WriteLine($"Parallel Engine:   {parallelTime}ms");
        Console.WriteLine($"Speedup:           {(double)sequentialTime / parallelTime:F2}x faster");
        Console.WriteLine($"Rules Evaluated:   {result2.TotalRulesEvaluated}");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    private static void DemoCompiledExpressions()
    {
        Console.WriteLine("🔥 Demo 2: Compiled Expression Evaluator");
        Console.WriteLine();

        var rule = Rule.Create(
            RuleId.Create("PERF_001"),
            "Performance Test",
            Expression.Create("age >= 18 && balance > 100"),
            Severity.Error,
            "Test rule",
            true);

        var input = new ValidationInputDto();
        input.Properties["age"] = 25;
        input.Properties["balance"] = 500;

        var comparisonEvaluator = new ComparisonExpressionEvaluator();
        var compiledEvaluator = new CompiledExpressionEvaluator();

        // Warm-up (compilar la expresión)
        compiledEvaluator.EvaluateAsync(rule, input).Wait();

        const int iterations = 10000;

        // Benchmark ComparisonExpressionEvaluator
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            comparisonEvaluator.EvaluateAsync(rule, input).Wait();
        }
        sw.Stop();
        var comparisonTime = sw.ElapsedMilliseconds;

        // Benchmark CompiledExpressionEvaluator
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            compiledEvaluator.EvaluateAsync(rule, input).Wait();
        }
        sw.Stop();
        var compiledTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Iterations:           {iterations:N0}");
        Console.WriteLine($"Comparison Evaluator: {comparisonTime}ms");
        Console.WriteLine($"Compiled Evaluator:   {compiledTime}ms");
        Console.WriteLine($"Speedup:              {(double)comparisonTime / compiledTime:F2}x faster");
        Console.WriteLine($"Cache Size:           {compiledEvaluator.GetCacheSize()} expressions");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    private static void DemoObjectPool()
    {
        Console.WriteLine("♻️  Demo 3: Object Pool");
        Console.WriteLine();

        // Pool de ValidationInputDto
        var pool = new ObjectPool<ValidationInputDto>(
            () => new ValidationInputDto(),
            dto => dto.Properties.Clear(), // Reset action
            maxSize: 50);

        const int iterations = 1000;

        // Sin pool (allocations directas)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var dto = new ValidationInputDto();
            dto.Properties["test"] = i;
            // GC se encarga de limpiar
        }
        sw.Stop();
        var directTime = sw.ElapsedMilliseconds;

        // Con pool
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            using var pooled = pool.RentScoped();
            pooled.Value.Properties["test"] = i;
            // Automáticamente se devuelve al pool
        }
        sw.Stop();
        var pooledTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Iterations:        {iterations:N0}");
        Console.WriteLine($"Direct Allocation: {directTime}ms");
        Console.WriteLine($"With ObjectPool:   {pooledTime}ms");
        Console.WriteLine($"Improvement:       {(double)directTime / pooledTime:F2}x faster");
        Console.WriteLine($"Pool Size:         {pool.Count} objects");
        Console.WriteLine();
        Console.WriteLine("💡 ObjectPool reduces GC pressure and allocation overhead");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    private static List<Rule> CreateTestRules(int count)
    {
        var rules = new List<Rule>();
        for (int i = 0; i < count; i++)
        {
            rules.Add(Rule.Create(
                RuleId.Create($"RULE_{i:D3}"),
                $"Test rule {i}",
                Expression.Create("age >= 18"),
                Severity.Error,
                $"Test message {i}",
                true));
        }
        return rules;
    }

    private static ValidationInputDto CreateTestInput()
    {
        var input = new ValidationInputDto();
        input.Properties["age"] = 25;
        input.Properties["balance"] = 500;
        input.Properties["status"] = "active";
        return input;
    }
}

/// <summary>
/// Benchmarks oficiales con BenchmarkDotNet.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class RuleEngineBenchmarks
{
    private Rule _testRule = null!;
    private ValidationInputDto _input = null!;
    private ComparisonExpressionEvaluator _comparisonEvaluator = null!;
    private CompiledExpressionEvaluator _compiledEvaluator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testRule = Rule.Create(
            RuleId.Create("BENCH_001"),
            "Benchmark Rule",
            Expression.Create("age >= 18 && balance > 100"),
            Severity.Error,
            "Benchmark",
            true);

        _input = new ValidationInputDto();
        _input.Properties["age"] = 25;
        _input.Properties["balance"] = 500;

        _comparisonEvaluator = new ComparisonExpressionEvaluator();
        _compiledEvaluator = new CompiledExpressionEvaluator();

        // Warm-up compiled evaluator
        _compiledEvaluator.EvaluateAsync(_testRule, _input).Wait();
    }

    [Benchmark(Baseline = true)]
    public bool ComparisonEvaluator()
    {
        return _comparisonEvaluator.EvaluateAsync(_testRule, _input).Result;
    }

    [Benchmark]
    public bool CompiledEvaluator()
    {
        return _compiledEvaluator.EvaluateAsync(_testRule, _input).Result;
    }
}

/// <summary>
/// Repositorio en memoria para testing.
/// </summary>
internal class InMemoryRuleRepository : IRuleRepository
{
    private readonly List<Rule> _rules;

    public InMemoryRuleRepository(List<Rule> rules)
    {
        _rules = rules;
    }

    public Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Rule>>(_rules);
    }

    public Task<IEnumerable<Rule>> LoadEnabledRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Rule>>(_rules.Where(r => r.IsEnabled));
    }

    public Task<Rule?> LoadRuleByIdAsync(RuleId ruleId, CancellationToken cancellationToken = default)
    {
        var rule = _rules.FirstOrDefault(r => r.Id.Equals(ruleId));
        return Task.FromResult(rule);
    }
}
