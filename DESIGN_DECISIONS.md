# Decisiones de Diseño - RuleEngineCLI

Este documento explica las decisiones arquitectónicas clave tomadas en el proyecto y su justificación.

## 1. Clean Architecture

### Decisión
Organizar el código en 4 capas claramente separadas: Domain, Application, Infrastructure, Presentation.

### Justificación
- **Separación de responsabilidades**: Cada capa tiene un propósito específico y bien definido
- **Independencia de frameworks**: El dominio no conoce nada de JSON, consola, o frameworks externos
- **Testabilidad**: Cada capa puede testearse de forma aislada
- **Mantenibilidad**: Cambios en una capa no afectan a las demás
- **Escalabilidad**: Fácil agregar nuevas features sin modificar el core

### Alternativas Consideradas
- **N-Tier tradicional**: Más simple pero acopla lógica de negocio con persistencia
- **Monolito sin capas**: Más rápido inicialmente pero difícil de mantener

## 2. Domain-Driven Design (DDD)

### Decisión
Usar Value Objects, Entities, y Aggregate Roots para modelar el dominio.

### Justificación
- **Value Objects (RuleId, Severity, Expression)**: Inmutables, validación en creación, semántica clara
- **Entities (Rule, RuleResult)**: Identidad única, comportamiento encapsulado
- **Aggregate Root (ValidationReport)**: Mantiene invariantes de negocio, punto único de acceso

### Beneficios Concretos
```csharp
// ❌ Antes (código procedural)
var ruleId = "RULE_001";
if (string.IsNullOrEmpty(ruleId)) throw new Exception();

// ✅ Después (Value Object)
var ruleId = RuleId.Create("RULE_001"); // Validación automática
```

## 3. Repository Pattern

### Decisión
Definir `IRuleRepository` en el Domain, implementar `JsonRuleRepository` en Infrastructure.

### Justificación
- **Abstracción de persistencia**: El dominio no sabe si las reglas vienen de JSON, DB, o API
- **Testabilidad**: Fácil crear mocks para tests unitarios
- **Flexibilidad**: Cambiar de JSON a SQL Server solo requiere nueva implementación de la interfaz

### Ejemplo de Extensibilidad
```csharp
// Cambio futuro: cargar reglas desde base de datos
public class SqlRuleRepository : IRuleRepository
{
    public async Task<IEnumerable<Rule>> LoadAllRulesAsync(...)
    {
        // SELECT * FROM Rules...
    }
}
```

## 4. Strategy Pattern para Expression Evaluator

### Decisión
Crear interfaz `IExpressionEvaluator` con implementación `ComparisonExpressionEvaluator`.

### Justificación
- **Open/Closed Principle**: Agregar nuevos evaluadores sin modificar código existente
- **Single Responsibility**: Cada evaluador se enfoca en un tipo de expresión
- **Composición**: Posible chain of responsibility con múltiples evaluadores

### Extensibilidad Futura
```csharp
public class RegexExpressionEvaluator : IExpressionEvaluator
{
    public bool CanEvaluate(Rule rule) => rule.Expression.Value.StartsWith("regex:");
    // Implementación para expresiones regex
}

public class CustomScriptEvaluator : IExpressionEvaluator
{
    public bool CanEvaluate(Rule rule) => rule.Expression.Value.StartsWith("script:");
    // Ejecutar scripts C# o JavaScript
}
```

## 5. Inmutabilidad en Value Objects

### Decisión
Todos los Value Objects son inmutables (solo getters, sin setters).

### Justificación
- **Thread-safe**: Múltiples threads pueden leer sin locks
- **Previsibilidad**: Un valor no cambia después de crearse
- **Igualdad por valor**: `ruleId1 == ruleId2` compara valores, no referencias
- **Evita bugs**: No hay estado mutable que pueda corromperse

### Comparación
```csharp
// ❌ Mutable (propenso a errores)
var severity = new Severity();
severity.Level = SeverityLevel.Error;
severity.Level = SeverityLevel.Info; // Cambió!

// ✅ Inmutable (seguro)
var severity = Severity.Error;
// No hay forma de cambiarlo, debe crear uno nuevo
```

## 6. Factory Methods en Entities

### Decisión
Usar método estático `Create()` en lugar de constructores públicos.

### Justificación
- **Validación centralizada**: Toda la lógica de validación en un lugar
- **Objetos siempre válidos**: No existen instancias inválidas en memoria
- **Expresividad**: `Rule.Create(...)` es más claro que `new Rule(...)`
- **Flexibilidad**: Fácil agregar lógica de creación compleja

### Ejemplo
```csharp
public static Rule Create(RuleId id, string description, ...)
{
    // Validaciones
    if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("Description required");
    
    if (description.Length > 500)
        throw new ArgumentException("Description too long");
    
    // Siempre retorna objeto válido
    return new Rule(id, description.Trim(), ...);
}
```

## 7. Dependency Injection Manual

### Decisión
Configurar DI manualmente usando `Microsoft.Extensions.DependencyInjection` en lugar de framework completo.

### Justificación
- **Control total**: Sabemos exactamente qué se inyecta y cómo
- **Educativo**: Demuestra comprensión de DI sin "magia"
- **Ligero**: No necesitamos ASP.NET Core completo para CLI
- **Testeable**: Fácil crear service providers para tests

### Composition Root
```csharp
public static IServiceProvider BuildServiceProvider(...)
{
    var services = new ServiceCollection();
    
    // Todas las dependencias registradas en un solo lugar
    services.AddSingleton<ILogger, ConsoleLogger>();
    services.AddSingleton<IRuleRepository, JsonRuleRepository>();
    services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
    services.AddSingleton<IRuleEngine, RuleEngine>();
    
    return services.BuildServiceProvider();
}
```

## 8. DTOs en Application Layer

### Decisión
Usar DTOs (`ValidationInputDto`, `ValidationReportDto`) en lugar de exponer entidades de dominio.

### Justificación
- **Protección del dominio**: Evita que capas externas modifiquen entidades
- **Versionado**: Cambiar DTOs sin afectar dominio
- **Serialización**: DTOs optimizados para JSON, entidades optimizadas para lógica
- **Mapeo explícito**: Conversión controlada entre domain y external

## 9. Exit Codes Significativos

### Decisión
Retornar códigos de salida diferentes según el resultado:
- `0`: PASS
- `1`: WARNING
- `2`: FAIL
- `99`: Error fatal

### Justificación
- **Integración CI/CD**: Scripts pueden actuar según el exit code
- **Automatización**: Fácil integrar en pipelines
- **Estándar Unix**: Seguir convenciones de CLI

### Uso en Scripts
```bash
#!/bin/bash
ruleengine -r rules.json -i data.json
EXIT_CODE=$?

if [ $EXIT_CODE -eq 2 ]; then
    echo "Validation FAILED - blocking deployment"
    exit 1
elif [ $EXIT_CODE -eq 1 ]; then
    echo "Validation WARNING - proceed with caution"
fi
```

## 10. Logging Estructurado

### Decisión
Crear abstracción `ILogger` con niveles (Debug, Info, Warning, Error).

### Justificación
- **Diagnóstico**: Modo debug para troubleshooting
- **Producción**: Info/Warning/Error para monitoreo
- **Abstracción**: Fácil cambiar de consola a archivo o servicio externo
- **Testeable**: Mock logger para verificar mensajes en tests

## 11. Result Objects vs Exceptions

### Decisión
Usar `RuleResult` para representar éxito/fallo en lugar de excepciones.

### Justificación
- **Flow control predecible**: Fallo de regla es caso de negocio, no excepcional
- **Performance**: Crear results es más rápido que throw/catch
- **Información rica**: Result contiene contexto completo del fallo
- **Functional approach**: Más cercano a paradigma funcional

### Comparación
```csharp
// ❌ Usando excepciones (no recomendado para flow control)
try {
    rule.Evaluate(data);
} catch (RuleFailedException ex) {
    // Manejar fallo
}

// ✅ Usando Result Objects
var result = rule.Evaluate(data);
if (!result.Passed) {
    // Manejar fallo con contexto completo
    Console.WriteLine(result.Message);
}
```

## 12. Separación de Concerns en CLI

### Decisión
Dividir CLI en: Program (entry point), InputParser (parsing), ReportFormatter (output), ServiceConfiguration (DI).

### Justificación
- **Single Responsibility**: Cada clase una responsabilidad
- **Testeable**: Parsers y formatters testeables sin ejecutar CLI completo
- **Reutilizable**: InputParser puede usarse en otras interfaces (API, GUI)

## Conclusión

Estas decisiones priorizan:
1. **Mantenibilidad a largo plazo** sobre rapidez inicial
2. **Testabilidad** en todos los niveles
3. **Extensibilidad** sin modificar código existente (OCP)
4. **Claridad** sobre cleverness

El resultado es un código que:
- Se entiende fácilmente
- Se prueba de forma aislada
- Se extiende sin romper existente
- Demuestra conocimiento profesional de arquitectura
