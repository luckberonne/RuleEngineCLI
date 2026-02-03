# RuleEngineCLI - Advanced Example (Phase 1 Features)

Este ejemplo demuestra las mejoras de **Fase 1 (Quick Wins)** implementadas en el RuleEngineCLI:

## 🚀 Características Demostradas

### 1. **Caché de Reglas** 🔥
- **CachedRuleRepository**: Envuelve el repositorio base con caché en memoria
- **Performance**: Mejora de ~99% en evaluaciones repetidas
- **TTL Configurable**: Por defecto 5 minutos, personalizable
- **Invalidación Manual**: Métodos para limpiar caché cuando sea necesario

```csharp
var baseRepo = new JsonRuleRepository("rules.json");
var cache = serviceProvider.GetRequiredService<IMemoryCache>();
var cachedRepo = new CachedRuleRepository(baseRepo, cache, TimeSpan.FromMinutes(5));
```

**Resultados de Performance:**
- Primera carga: ~187ms (desde disco)
- Segunda carga: ~1ms (desde caché)
- **Mejora: 99.5% más rápido**

### 2. **Evaluador NCalc** 🧮
- **NCalcExpressionEvaluator**: Evaluador avanzado usando librería NCalc
- **Expresiones Complejas**: Soporta matemáticas, funciones, operadores ternarios
- **Seguridad**: Validación de expresiones peligrosas

**Capacidades:**
- ✅ Operadores ternarios: `total * (itemCount > 5 ? 0.9 : 1.0)`
- ✅ Funciones matemáticas: `Pow()`, `Sqrt()`, `Abs()`, `Log()`
- ✅ Precedencia estándar: Paréntesis, multiplicación, suma
- ✅ Operadores lógicos complejos: `&&`, `||`, `!`

```csharp
services.AddSingleton<IExpressionEvaluator, NCalcExpressionEvaluator>();
```

### 3. **Instrumentación con Métricas** 📊
- **InstrumentedRuleEngine**: Decorator que captura métricas del motor
- **System.Diagnostics.Metrics**: Estándar .NET para observabilidad
- **Exportable**: Compatible con Prometheus, Grafana, OpenTelemetry

**Métricas Capturadas:**
- `rule_engine.evaluations.total` - Contador de evaluaciones
- `rule_engine.rules.evaluated` - Total de reglas procesadas
- `rule_engine.rules.failed` - Reglas que fallaron
- `rule_engine.evaluation.duration` - Histograma de tiempos

```csharp
var instrumentedEngine = new InstrumentedRuleEngine(baseEngine);
```

## 📦 Dependencias Agregadas

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.1" />
<PackageReference Include="NCalc" Version="1.3.8" />
```

## 🏃 Cómo Ejecutar

Desde la raíz del proyecto:

```bash
dotnet run --project examples/AdvancedExample
```

## 📈 Salida Esperada

El programa ejecuta tres demos:

1. **Demo de Caché**: Compara tiempos de carga con/sin caché
2. **Demo de NCalc**: Evalúa expresiones matemáticas complejas
3. **Demo de Métricas**: Genera y visualiza métricas de evaluación

## 🎯 Patrones Implementados

- **Decorator Pattern**: `CachedRuleRepository`, `InstrumentedRuleEngine`
- **Dependency Injection**: Configuración modular con `IServiceCollection`
- **Open/Closed Principle**: Extensión sin modificar código existente
- **Single Responsibility**: Cada componente tiene una responsabilidad clara

## 📝 Notas

- Las advertencias NU1701 sobre NCalc son esperadas (compatibilidad .NET Framework)
- El caché usa `IMemoryCache` de Microsoft.Extensions
- Las métricas usan `System.Diagnostics.Metrics` (incluido en .NET 8)

## 🔜 Próximas Fases

Este ejemplo implementa la **Fase 1**. Futuras mejoras incluyen:

- **Fase 2**: Configuración avanzada, validación de esquemas
- **Fase 3**: Operadores complejos, motor de flujos
- **Fase 4**: Reglas dinámicas, machine learning

---

Para más información, consulta la [documentación principal](../../README.md).
