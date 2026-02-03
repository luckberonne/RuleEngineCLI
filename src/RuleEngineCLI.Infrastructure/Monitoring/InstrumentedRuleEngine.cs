using System.Diagnostics;
using System.Diagnostics.Metrics;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;

namespace RuleEngineCLI.Infrastructure.Monitoring;

/// <summary>
/// Implementación de IRuleEngine que instrumenta las operaciones con métricas.
/// Usa System.Diagnostics.Metrics para OpenTelemetry/Prometheus.
/// </summary>
public sealed class InstrumentedRuleEngine : IRuleEngine
{
    private readonly IRuleEngine _innerEngine;
    private readonly Meter _meter;
    private readonly Counter<long> _evaluationsCounter;
    private readonly Histogram<double> _evaluationDuration;
    private readonly Counter<long> _rulesEvaluatedCounter;
    private readonly Counter<long> _rulesFailedCounter;

    public InstrumentedRuleEngine(IRuleEngine innerEngine, string? meterName = null)
    {
        _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        
        _meter = new Meter(meterName ?? "RuleEngineCLI", "1.0.0");
        
        // Contadores
        _evaluationsCounter = _meter.CreateCounter<long>(
            name: "rule_engine.evaluations.total",
            unit: "evaluations",
            description: "Total number of rule evaluations");

        _rulesEvaluatedCounter = _meter.CreateCounter<long>(
            name: "rule_engine.rules.evaluated",
            unit: "rules",
            description: "Total number of individual rules evaluated");

        _rulesFailedCounter = _meter.CreateCounter<long>(
            name: "rule_engine.rules.failed",
            unit: "rules",
            description: "Total number of rules that failed");

        // Histograma para medir duración
        _evaluationDuration = _meter.CreateHistogram<double>(
            name: "rule_engine.evaluation.duration",
            unit: "ms",
            description: "Duration of rule evaluation in milliseconds");
    }

    public async Task<ValidationReportDto> EvaluateAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        ValidationReportDto? report = null;

        try
        {
            report = await _innerEngine.EvaluateAsync(input, cancellationToken);
            return report;
        }
        finally
        {
            stopwatch.Stop();
            RecordMetrics(report, stopwatch.Elapsed);
        }
    }

    public async Task<ValidationReportDto> EvaluateEnabledRulesAsync(
        ValidationInputDto input,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        ValidationReportDto? report = null;

        try
        {
            report = await _innerEngine.EvaluateEnabledRulesAsync(input, cancellationToken);
            return report;
        }
        finally
        {
            stopwatch.Stop();
            RecordMetrics(report, stopwatch.Elapsed);
        }
    }

    private void RecordMetrics(ValidationReportDto? report, TimeSpan elapsed)
    {
        if (report == null)
            return;

        var tags = new TagList
        {
            { "status", report.Status.ToLowerInvariant() },
            { "max_severity", report.MaxSeverity.ToLowerInvariant() }
        };

        // Incrementar contadores
        _evaluationsCounter.Add(1, tags);
        _rulesEvaluatedCounter.Add(report.TotalRulesEvaluated, tags);
        _rulesFailedCounter.Add(report.TotalFailed, tags);

        // Registrar duración
        _evaluationDuration.Record(elapsed.TotalMilliseconds, tags);
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
