# RuleEngineCLI - Configuration Example (Phase 2 Features)

Este ejemplo demuestra las mejoras de **Fase 2 (Configuración Avanzada)** implementadas en el RuleEngineCLI:

## 🚀 Características Demostradas

### 1. **Sistema de Configuración Tipado** ⚙️
- **RuleEngineOptions**: Configuración fuertemente tipada
- **appsettings.json**: Configuración base
- **appsettings.{Environment}.json**: Sobrescritura por entorno
- **Variables de entorno**: Soporte completo

#### Estructura de Configuración

```json
{
  "RuleEngine": {
    "RulesFilePath": "../../examples/rules.json",
    "ValidateSchema": true,
    "Cache": {
      "Enabled": true,
      "ExpirationMinutes": 5,
      "MaxSize": 100
    },
    "Logging": {
      "MinimumLevel": "Information",
      "IncludeTimestamp": true,
      "IncludeExceptionDetails": true,
      "Format": "Console"
    },
    "Evaluation": {
      "EvaluatorType": "NCalc",
      "ContinueOnError": true,
      "TimeoutSeconds": 30,
      "EnableMetrics": true
    }
  }
}
```

### 2. **Validación de Esquema JSON** 📋
- **JsonSchemaValidator**: Valida estructura de rules.json
- **Validaciones**:
  - ✅ Propiedades requeridas (id, name, expression, severity)
  - ✅ Tipos de datos correctos
  - ✅ Valores de severidad válidos
  - ✅ Estructura JSON bien formada

```csharp
var validator = new JsonSchemaValidator();
var result = await validator.ValidateRulesFileAsync("rules.json");

if (result.IsValid)
{
    Console.WriteLine("✅ Schema validation passed!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"❌ {error}");
    }
}
```

### 3. **Logging Estructurado** 📝
- **StructuredLogger**: Logger con múltiples formatos
- **Formatos soportados**:
  - `Console`: Formato legible con colores
  - `Structured`: Formato key-value para parseo
  - `Json`: Formato JSON para agregadores de logs

#### Formato Console
```
[2026-02-03 14:47:13] [INFO] Starting rule evaluation process...
[2026-02-03 14:47:13] [ERROR] Error evaluating rule RULE_002
```

#### Formato Structured
```
Timestamp="2026-02-03T14:47:13.0447670Z" Level="INFO" Message="Starting rule evaluation process..."
```

#### Formato JSON
```json
{"timestamp":"2026-02-03T14:47:13.0447670Z","level":"INFO","message":"Starting rule evaluation process..."}
```

### 4. **Configuración Multi-Entorno** 🎭

#### Development (appsettings.Development.json)
```json
{
  "RuleEngine": {
    "Logging": {
      "MinimumLevel": "Debug",
      "Format": "Structured"
    },
    "Evaluation": {
      "EnableMetrics": true
    }
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "RuleEngine": {
    "Cache": {
      "ExpirationMinutes": 30
    },
    "Logging": {
      "MinimumLevel": "Warning",
      "Format": "Json"
    },
    "Evaluation": {
      "ContinueOnError": false
    }
  }
}
```

## 📦 Dependencias Agregadas

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.2" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.0.0" />
```

## 🏃 Cómo Ejecutar

### Environment: Development (default)
```bash
cd examples/ConfigurationExample
dotnet run
```

### Environment: Production
```bash
cd examples/ConfigurationExample
$env:DOTNET_ENVIRONMENT="Production"
dotnet run
```

### Variables de Entorno Personalizadas
```bash
$env:RuleEngine__Logging__MinimumLevel="Debug"
$env:RuleEngine__Cache__ExpirationMinutes="10"
dotnet run
```

## 📈 Salida Esperada

El programa ejecuta tres demos:

1. **Demo de Validación de Esquema**: Valida rules.json y muestra errores si existen
2. **Demo de Logging Estructurado**: Muestra logs en diferentes niveles y formatos
3. **Demo de Configuración por Entorno**: Explica diferencias entre Development/Production

## 🎯 Patrones Implementados

- **Options Pattern**: Configuración fuertemente tipada con `RuleEngineOptions`
- **Configuration Builder**: Carga jerárquica de configuración
- **Environment-Specific Config**: Sobrescritura por entorno
- **Structured Logging**: Logs parseables y agregables
- **Schema Validation**: Validación temprana de datos

## 🔧 Opciones de Configuración

### RuleEngineOptions

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `RulesFilePath` | string | "rules.json" | Ruta al archivo de reglas |
| `ValidateSchema` | bool | true | Validar esquema antes de cargar |

### CacheOptions

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Enabled` | bool | true | Habilitar caché de reglas |
| `ExpirationMinutes` | int | 5 | TTL del caché en minutos |
| `MaxSize` | int? | null | Tamaño máximo (null = ilimitado) |

### LoggingOptions

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `MinimumLevel` | string | "Information" | Debug, Information, Warning, Error |
| `IncludeTimestamp` | bool | true | Incluir timestamp en logs |
| `IncludeExceptionDetails` | bool | true | Incluir detalles de excepciones |
| `Format` | string | "Console" | Console, Structured, Json |

### EvaluationOptions

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `EvaluatorType` | string | "Comparison" | Comparison, NCalc |
| `ContinueOnError` | bool | true | Continuar si una regla falla |
| `TimeoutSeconds` | int | 30 | Timeout para evaluación |
| `EnableMetrics` | bool | false | Habilitar métricas |

## 📝 Notas

- La configuración sigue el patrón jerárquico de .NET
- Los archivos de configuración se copian al output al compilar
- Las variables de entorno usan `__` (doble underscore) como separador
- El validador de esquema detecta errores comunes antes de runtime

## 🔜 Próximas Fases

Este ejemplo implementa la **Fase 2**. Futuras mejoras incluyen:

- **Fase 3**: Operadores avanzados (RegEx, In, Between, IsNull)
- **Fase 4**: Workflows de reglas, ML.NET integration

---

Para más información, consulta la [documentación principal](../../README.md).
