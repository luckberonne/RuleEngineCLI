# RuleEngineCLI - Guía de Consumo como Librería/API

Esta guía muestra todas las formas de consumir RuleEngineCLI en tus aplicaciones .NET.

## 📚 Formas de Consumo

### 1. **Integración Básica como Librería**
### 2. **API REST con ASP.NET Core**
### 3. **Servicio Windows/Background**
### 4. **Azure Functions/Serverless**
### 5. **Microservicio con Docker**

---

## 🔧 1. INTEGRACIÓN BÁSICA COMO LIBRERÍA

### Configuración Mínima

```csharp
using Microsoft.Extensions.DependencyInjection;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

public class RuleEngineService
{
    private readonly IRuleEngine _ruleEngine;

    public RuleEngineService()
    {
        // Configurar servicios manualmente
        var services = new ServiceCollection();

        // Repositorio de reglas (desde archivo JSON)
        services.AddSingleton<IRuleRepository>(sp =>
            new JsonRuleRepository("rules.json"));

        // Evaluador de expresiones
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();

        // Logger
        services.AddSingleton<ILogger, ConsoleLogger>();

        // Motor de reglas
        services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();

        var serviceProvider = services.BuildServiceProvider();
        _ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();
    }

    public async Task<ValidationReportDto> ValidateData(Dictionary<string, object> data)
    {
        var input = new ValidationInputDto(data);
        return await _ruleEngine.EvaluateAsync(input);
    }
}
```

### Uso Simple

```csharp
var service = new RuleEngineService();

var data = new Dictionary<string, object>
{
    ["age"] = 25,
    ["income"] = 50000,
    ["creditScore"] = 750
};

var result = await service.ValidateData(data);

if (result.Status == "PASS")
{
    Console.WriteLine("✅ Datos válidos");
}
else
{
    Console.WriteLine("❌ Errores encontrados:");
    foreach (var failure in result.Results.Where(r => !r.Passed))
    {
        Console.WriteLine($"  • {failure.Message}");
    }
}
```

---

## 🌐 2. API REST CON ASP.NET CORE

### Program.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar RuleEngineCLI
builder.Services.AddSingleton<IRuleRepository>(sp =>
    new JsonRuleRepository("rules.json"));
builder.Services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
builder.Services.AddSingleton<ILogger, ConsoleLogger>();
builder.Services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();

var app = builder.Build();

app.MapPost("/api/validate", async (ValidationRequest request, IRuleEngine ruleEngine) =>
{
    var input = new ValidationInputDto(request.Data);
    var result = await ruleEngine.EvaluateAsync(input);

    return Results.Ok(new
    {
        Status = result.Status,
        Passed = result.TotalPassed,
        Failed = result.TotalFailed,
        MaxSeverity = result.MaxSeverity,
        Results = result.Results
    });
});

app.Run();

public record ValidationRequest(Dictionary<string, object> Data);
```

### Uso con HTTP

```bash
# Validar datos
curl -X POST http://localhost:5000/api/validate \
  -H "Content-Type: application/json" \
  -d '{
    "data": {
      "age": 25,
      "income": 50000,
      "creditScore": 750
    }
  }'
```

### Respuesta JSON

```json
{
  "status": "PASS",
  "passed": 5,
  "failed": 0,
  "maxSeverity": "INFO",
  "results": [
    {
      "ruleId": "AGE_CHECK",
      "description": "Age must be 18 or older",
      "passed": true,
      "severity": "ERROR",
      "message": "",
      "evaluatedAt": "2026-02-03T15:30:00Z"
    }
  ]
}
```

---

## ⚙️ 3. SERVICIO WINDOWS/BACKGROUND

### Servicio Windows

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class RuleEngineBackgroundService : BackgroundService
{
    private readonly IRuleEngine _ruleEngine;
    private readonly ILogger<RuleEngineBackgroundService> _logger;

    public RuleEngineBackgroundService(IRuleEngine ruleEngine, ILogger<RuleEngineBackgroundService> logger)
    {
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Procesar cola de mensajes o archivos
                await ProcessPendingValidations();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing validations");
            }
        }
    }

    private async Task ProcessPendingValidations()
    {
        // Leer de base de datos, cola, o sistema de archivos
        // Procesar con RuleEngineCLI
        // Guardar resultados
    }
}
```

### Program.cs para Servicio

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Configurar RuleEngineCLI
        services.AddSingleton<IRuleRepository>(sp =>
            new JsonRuleRepository("rules.json"));
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();

        // Registrar servicio background
        services.AddHostedService<RuleEngineBackgroundService>();
    })
    .Build();

await host.RunAsync();
```

---

## ☁️ 4. AZURE FUNCTIONS/SERVERLESS

### Function HTTP Trigger

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;
using System.Net;

public class RuleEngineFunction
{
    private readonly IRuleEngine _ruleEngine;

    public RuleEngineFunction(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    [Function("ValidateData")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RuleEngineFunction");

        try
        {
            // Leer datos del request
            var requestBody = await req.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            // Validar con RuleEngineCLI
            var input = new ValidationInputDto(data);
            var result = await _ruleEngine.EvaluateAsync(input);

            // Retornar respuesta
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating data");

            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Error processing validation request");
            return errorResponse;
        }
    }
}
```

### Program.cs para Azure Functions

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Configurar RuleEngineCLI
        services.AddSingleton<IRuleRepository>(sp =>
            new JsonRuleRepository("rules.json"));
        services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();
    })
    .Build();

host.Run();
```

---

## 🐳 5. MICROSERVICIO CON DOCKER

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["RuleEngineService/RuleEngineService.csproj", "RuleEngineService/"]
COPY ["RuleEngineCLI/src/RuleEngineCLI.Application/RuleEngineCLI.Application.csproj", "RuleEngineCLI/src/RuleEngineCLI.Application/"]
COPY ["RuleEngineCLI/src/RuleEngineCLI.Domain/RuleEngineCLI.Domain.csproj", "RuleEngineCLI/src/RuleEngineCLI.Domain/"]
COPY ["RuleEngineCLI/src/RuleEngineCLI.Infrastructure/RuleEngineCLI.Infrastructure.csproj", "RuleEngineCLI/src/RuleEngineCLI.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "RuleEngineService/RuleEngineService.csproj"

# Copiar código fuente
COPY . .

# Build
WORKDIR "/src/RuleEngineService"
RUN dotnet build "RuleEngineService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RuleEngineService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RuleEngineService.dll"]
```

### API Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Application.Services;

[ApiController]
[Route("api/[controller]")]
public class ValidationController : ControllerBase
{
    private readonly IRuleEngine _ruleEngine;

    public ValidationController(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidationRequest request)
    {
        var input = new ValidationInputDto(request.Data);
        var result = await _ruleEngine.EvaluateAsync(input);

        return Ok(new ValidationResponse
        {
            RequestId = Guid.NewGuid(),
            Status = result.Status,
            TotalRules = result.TotalRulesEvaluated,
            Passed = result.TotalPassed,
            Failed = result.TotalFailed,
            MaxSeverity = result.MaxSeverity,
            Results = result.Results,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("validate-batch")]
    public async Task<IActionResult> ValidateBatch([FromBody] BatchValidationRequest request)
    {
        var results = new List<ValidationResponse>();

        foreach (var item in request.Items)
        {
            var input = new ValidationInputDto(item.Data);
            var result = await _ruleEngine.EvaluateAsync(input);

            results.Add(new ValidationResponse
            {
                RequestId = item.Id,
                Status = result.Status,
                TotalRules = result.TotalRulesEvaluated,
                Passed = result.TotalPassed,
                Failed = result.TotalFailed,
                MaxSeverity = result.MaxSeverity,
                Results = result.Results,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new BatchValidationResponse
        {
            BatchId = Guid.NewGuid(),
            TotalItems = results.Count,
            Results = results,
            ProcessedAt = DateTime.UtcNow
        });
    }
}

public record ValidationRequest(Dictionary<string, object> Data);
public record ValidationResponse(
    Guid RequestId,
    string Status,
    int TotalRules,
    int Passed,
    int Failed,
    string MaxSeverity,
    List<RuleResultDto> Results,
    DateTime Timestamp
);
public record BatchValidationRequest(List<BatchItem> Items);
public record BatchItem(Guid Id, Dictionary<string, object> Data);
public record BatchValidationResponse(
    Guid BatchId,
    int TotalItems,
    List<ValidationResponse> Results,
    DateTime ProcessedAt
);
```

### Docker Compose

```yaml
version: '3.8'
services:
  ruleengine-api:
    build: .
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./rules:/app/rules:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

---

## 🔄 6. INTEGRACIÓN CON CACHE Y BASES DE DATOS

### Con Redis Cache

```csharp
using Microsoft.Extensions.Caching.Distributed;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

public class CachedRuleRepository : IRuleRepository
{
    private readonly IRuleRepository _innerRepository;
    private readonly IDistributedCache _cache;

    public CachedRuleRepository(IRuleRepository innerRepository, IDistributedCache cache)
    {
        _innerRepository = innerRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<Rule>> LoadAllRulesAsync()
    {
        const string cacheKey = "rules:all";

        // Intentar obtener del cache
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<IEnumerable<Rule>>(cached);
        }

        // Obtener de la fuente original
        var rules = await _innerRepository.LoadAllRulesAsync();

        // Guardar en cache por 5 minutos
        var serialized = JsonSerializer.Serialize(rules);
        await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return rules;
    }
}
```

### Con Entity Framework

```csharp
using Microsoft.EntityFrameworkCore;
using RuleEngineCLI.Domain.Entities;

public class EfRuleRepository : IRuleRepository
{
    private readonly RuleEngineDbContext _context;

    public EfRuleRepository(RuleEngineDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Rule>> LoadAllRulesAsync()
    {
        return await _context.Rules
            .Where(r => r.IsEnabled)
            .ToListAsync();
    }

    public async Task<IEnumerable<Rule>> LoadEnabledRulesAsync()
    {
        return await _context.Rules
            .Where(r => r.IsEnabled)
            .ToListAsync();
    }
}

public class RuleEngineDbContext : DbContext
{
    public DbSet<Rule> Rules { get; set; }

    public RuleEngineDbContext(DbContextOptions<RuleEngineDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasConversion(id => id.ToString(), str => RuleId.Create(str));
            // ... otras configuraciones
        });
    }
}
```

---

## 📊 7. MONITOREO Y OBSERVABILIDAD

### Con Application Insights

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RuleEngineCLI.Application.Services;

public class InstrumentedRuleEngine : IRuleEngine
{
    private readonly IRuleEngine _innerEngine;
    private readonly TelemetryClient _telemetry;

    public InstrumentedRuleEngine(IRuleEngine innerEngine, TelemetryConfiguration telemetryConfig)
    {
        _innerEngine = innerEngine;
        _telemetry = new TelemetryClient(telemetryConfig);
    }

    public async Task<ValidationReportDto> EvaluateAsync(ValidationInputDto input)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _innerEngine.EvaluateAsync(input);

            // Métricas personalizadas
            _telemetry.TrackMetric("RuleEngine.Evaluation.Duration", (DateTime.UtcNow - startTime).TotalMilliseconds);
            _telemetry.TrackMetric("RuleEngine.Rules.Evaluated", result.TotalRulesEvaluated);
            _telemetry.TrackMetric("RuleEngine.Rules.Passed", result.TotalPassed);
            _telemetry.TrackMetric("RuleEngine.Rules.Failed", result.TotalFailed);

            // Eventos personalizados
            _telemetry.TrackEvent("RuleEngine.Evaluation.Completed", new Dictionary<string, string>
            {
                ["Status"] = result.Status,
                ["MaxSeverity"] = result.MaxSeverity
            });

            return result;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

---

## 🚀 8. CONFIGURACIÓN AVANZADA

### appsettings.json

```json
{
  "RuleEngine": {
    "RulesFilePath": "rules/validation-rules.json",
    "ValidateSchema": true,
    "Cache": {
      "Enabled": true,
      "ExpirationMinutes": 30,
      "MaxSize": 1000
    },
    "Logging": {
      "MinimumLevel": "Information",
      "IncludeTimestamp": true,
      "IncludeExceptionDetails": true,
      "Format": "JSON"
    },
    "Evaluation": {
      "EvaluatorType": "NCalc",
      "ContinueOnError": true,
      "TimeoutSeconds": 30,
      "EnableMetrics": true
    },
    "Database": {
      "ConnectionString": "Server=.;Database=RuleEngine;Trusted_Connection=True;",
      "EnableMigrations": true
    }
  }
}
```

### Configuración Tipada

```csharp
public class RuleEngineOptions
{
    public const string SectionName = "RuleEngine";

    public string RulesFilePath { get; set; } = "rules.json";
    public bool ValidateSchema { get; set; } = true;
    public CacheOptions Cache { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public EvaluationOptions Evaluation { get; set; } = new();
    public DatabaseOptions Database { get; set; } = new();
}

public class CacheOptions
{
    public bool Enabled { get; set; } = true;
    public int ExpirationMinutes { get; set; } = 5;
    public int MaxSize { get; set; } = 100;
}

public class EvaluationOptions
{
    public string EvaluatorType { get; set; } = "Comparison";
    public bool ContinueOnError { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableMetrics { get; set; } = false;
}
```

---

## 📈 9. PRUEBAS UNITARIAS E INTEGRACIÓN

### Tests Unitarios

```csharp
using Xunit;
using Moq;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Application.DTOs;

public class RuleEngineServiceTests
{
    [Fact]
    public async Task EvaluateAsync_ValidData_ReturnsPass()
    {
        // Arrange
        var mockRuleEngine = new Mock<IRuleEngine>();
        mockRuleEngine.Setup(x => x.EvaluateAsync(It.IsAny<ValidationInputDto>()))
            .ReturnsAsync(new ValidationReportDto
            {
                Status = "PASS",
                TotalRulesEvaluated = 3,
                TotalPassed = 3,
                TotalFailed = 0,
                MaxSeverity = "INFO",
                Results = new List<RuleResultDto>()
            });

        var service = new RuleEngineService(mockRuleEngine.Object);

        // Act
        var result = await service.ValidateData(new Dictionary<string, object>
        {
            ["age"] = 25,
            ["income"] = 50000
        });

        // Assert
        Assert.Equal("PASS", result.Status);
        Assert.Equal(3, result.TotalPassed);
    }
}
```

### Tests de Integración

```csharp
[Fact]
public async Task FullIntegrationTest_WithRealFiles()
{
    // Configurar servicios reales
    var services = new ServiceCollection();
    services.AddSingleton<IRuleRepository>(sp =>
        new JsonRuleRepository("test-rules.json"));
    services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
    services.AddSingleton<ILogger, ConsoleLogger>();
    services.AddSingleton<IRuleEngine, RuleEngineCLI.Application.Implementation.RuleEngine>();

    var serviceProvider = services.BuildServiceProvider();
    var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

    // Ejecutar validación completa
    var input = new ValidationInputDto(new Dictionary<string, object>
    {
        ["age"] = 25,
        ["income"] = 50000,
        ["creditScore"] = 750
    });

    var result = await ruleEngine.EvaluateAsync(input);

    // Verificar resultados
    Assert.Equal("PASS", result.Status);
    Assert.True(result.TotalPassed > 0);
}
```

---

## 🎯 RESUMEN DE FORMAS DE CONSUMO

| Método | Complejidad | Casos de Uso | Escalabilidad |
|--------|-------------|--------------|---------------|
| **Librería Básica** | Baja | Aplicaciones simples | Media |
| **API REST** | Media | Microservicios, web apps | Alta |
| **Servicio Background** | Media | Procesamiento batch | Alta |
| **Azure Functions** | Baja | Serverless, event-driven | Muy Alta |
| **Docker** | Alta | Contenedores, cloud | Muy Alta |
| **Con Cache/DB** | Alta | Enterprise, alta carga | Muy Alta |

Cada método tiene sus ventajas dependiendo de tus necesidades específicas de arquitectura y escalabilidad.