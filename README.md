# RuleEngineCLI - Configurable Business Rules Validation Engine

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-green)
![DDD](https://img.shields.io/badge/Design-DDD-orange)

## 📋 Descripción

**RuleEngineCLI** es una aplicación de consola profesional diseñada para validar datos de entrada contra un conjunto de reglas de negocio configurables. Implementa Clean Architecture y Domain-Driven Design (DDD), permitiendo cambiar reglas sin modificar el código fuente.

### Casos de Uso
- ✅ Validación de datos en procesos de registro
- ✅ Compliance y auditoría
- ✅ QA y testing de lógica de negocio
- ✅ Validación pre-procesamiento de datos

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────┐
│            Presentation Layer (CLI)                 │
│  - Command line parsing                             │
│  - Input/Output formatting                          │
│  - Dependency injection setup                       │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│            Application Layer                        │
│  - Use Cases (EvaluateRulesUseCase)                 │
│  - DTOs (ValidationInputDto, ValidationReportDto)   │
│  - Service Interfaces (IRuleEngine, ILogger)        │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│            Domain Layer (Core)                      │
│  - Entities (Rule, RuleResult, ValidationReport)    │
│  - Value Objects (RuleId, Severity, Expression)     │
│  - Repository Interfaces (IRuleRepository)          │
│  - Business Logic                                   │
└────────────────┬────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────┐
│            Infrastructure Layer                     │
│  - JsonRuleRepository (file system)                 │
│  - ComparisonExpressionEvaluator                    │
│  - ConsoleLogger                                    │
│  - JSON Serialization                               │
└─────────────────────────────────────────────────────┘
```

### Flujo de Ejecución

1. **CLI** recibe argumentos (--rules, --input)
2. **DI Container** configura todas las dependencias
3. **InputParser** carga y parsea los datos de entrada
4. **EvaluateRulesUseCase** orquesta el proceso:
   - Carga reglas desde **JsonRuleRepository**
   - Evalúa cada regla usando **ExpressionEvaluator**
   - Crea **RuleResult** para cada evaluación
   - Genera **ValidationReport** con estado final
5. **ReportFormatter** presenta resultados en consola
6. Retorna exit code apropiado (0=PASS, 1=WARNING, 2=FAIL)

## 🎯 Principios SOLID Aplicados

### Single Responsibility Principle (SRP)
- **Rule**: Solo maneja lógica de una regla individual
- **ValidationReport**: Solo agrega resultados y calcula estado
- **EvaluateRulesUseCase**: Solo orquesta la evaluación

### Open/Closed Principle (OCP)
- Nuevos evaluadores de expresiones pueden añadirse implementando `IExpressionEvaluator`
- Nuevos repositorios sin modificar el dominio

### Liskov Substitution Principle (LSP)
- Todas las implementaciones de interfaces son intercambiables
- Value Objects son inmutables y sustituibles

### Interface Segregation Principle (ISP)
- `IRuleEngine`: solo métodos de evaluación
- `ILogger`: solo métodos de logging
- `IRuleRepository`: solo métodos de acceso a reglas

### Dependency Inversion Principle (DIP)
- Todas las capas dependen de abstracciones, no de implementaciones
- Domain define interfaces, Infrastructure las implementa

## 📂 Estructura del Proyecto

```
RuleEngineCLI/
├── src/
│   ├── RuleEngineCLI.Domain/
│   │   ├── Entities/
│   │   │   ├── Rule.cs
│   │   │   ├── RuleResult.cs
│   │   │   └── ValidationReport.cs
│   │   ├── ValueObjects/
│   │   │   ├── RuleId.cs
│   │   │   ├── Severity.cs
│   │   │   └── Expression.cs
│   │   └── Repositories/
│   │       └── IRuleRepository.cs
│   │
│   ├── RuleEngineCLI.Application/
│   │   ├── UseCases/
│   │   │   └── EvaluateRulesUseCase.cs
│   │   ├── DTOs/
│   │   │   ├── ValidationInputDto.cs
│   │   │   └── ValidationReportDto.cs
│   │   ├── Services/
│   │   │   ├── IRuleEngine.cs
│   │   │   ├── IExpressionEvaluator.cs
│   │   │   └── ILogger.cs
│   │   └── Implementation/
│   │       └── RuleEngine.cs
│   │
│   ├── RuleEngineCLI.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Models/
│   │   │   │   └── RuleJsonModel.cs
│   │   │   ├── Mappers/
│   │   │   │   └── RuleMapper.cs
│   │   │   └── Repositories/
│   │   │       └── JsonRuleRepository.cs
│   │   ├── Evaluation/
│   │   │   └── ComparisonExpressionEvaluator.cs
│   │   └── Logging/
│   │       └── ConsoleLogger.cs
│   │
│   └── RuleEngineCLI.Presentation.CLI/
│       ├── Program.cs
│       ├── DependencyInjection/
│       │   └── ServiceConfiguration.cs
│       └── Utilities/
│           ├── InputParser.cs
│           └── ReportFormatter.cs
│
├── tests/
│   ├── RuleEngineCLI.Domain.Tests/
│   ├── RuleEngineCLI.Application.Tests/
│   └── RuleEngineCLI.Infrastructure.Tests/
│
├── examples/
│   ├── rules.json
│   ├── valid-input.json
│   ├── invalid-input.json
│   └── warning-input.json
│
└── RuleEngineCLI.sln
```

## 🚀 Uso

### Compilar el Proyecto

```bash
cd RuleEngineCLI
dotnet build
```

### Ejecutar Validación

```bash
# Usando archivos JSON
dotnet run --project src/RuleEngineCLI.Presentation.CLI -- \
  --rules examples/rules.json \
  --input examples/valid-input.json \
  --verbose

# Usando datos inline
dotnet run --project src/RuleEngineCLI.Presentation.CLI -- \
  --rules examples/rules.json \
  --data '{"age": 25, "balance": 100}' \
  --verbose

# Modo debug
dotnet run --project src/RuleEngineCLI.Presentation.CLI -- \
  --rules examples/rules.json \
  --input examples/invalid-input.json \
  --debug
```

### Opciones de Línea de Comandos

| Opción | Alias | Descripción | Requerido |
|--------|-------|-------------|-----------|
| `--rules` | `-r` | Ruta al archivo de reglas JSON | ✅ |
| `--input` | `-i` | Ruta al archivo de datos de entrada JSON | ❌* |
| `--data` | `-d` | Datos JSON inline como string | ❌* |
| `--verbose` | `-v` | Mostrar todas las reglas evaluadas | ❌ |
| `--debug` | | Habilitar logging de debug | ❌ |
| `--only-enabled` | | Evaluar solo reglas habilitadas (default: true) | ❌ |

*Nota: Debes proporcionar `--input` O `--data`, no ambos.

### Exit Codes

- `0`: PASS - Todas las reglas pasaron
- `1`: WARNING - Algunas reglas fallaron con severidad WARNING
- `2`: FAIL - Una o más reglas fallaron con severidad ERROR
- `99`: Error fatal en la ejecución

## 📋 Formato de Reglas (JSON)

```json
{
  "version": "1.0.0",
  "metadata": {
    "description": "Business validation rules",
    "lastUpdated": "2026-02-02"
  },
  "rules": [
    {
      "id": "RULE_001",
      "description": "User age must be 18 or older",
      "expression": "age >= 18",
      "severity": "ERROR",
      "errorMessage": "User must be at least 18 years old.",
      "isEnabled": true
    }
  ]
}
```

### Expresiones Soportadas

| Operador | Descripción | Ejemplo |
|----------|-------------|---------|
| `==` | Igual a | `status == "active"` |
| `!=` | Diferente de | `role != "admin"` |
| `>` | Mayor que | `age > 18` |
| `<` | Menor que | `price < 100` |
| `>=` | Mayor o igual | `score >= 75` |
| `<=` | Menor o igual | `quantity <= 10` |
| `&&` | AND lógico | `isActive == true && isVerified == true` |
| `\|\|` | OR lógico | `role == "admin" \|\| role == "superadmin"` |

### Tipos de Datos Soportados

- **Números**: `10`, `3.14`, `-5`
- **Strings**: `"value"`, `'value'`
- **Booleanos**: `true`, `false`
- **Fechas**: `"2026-01-01"` (formato ISO 8601)
- **Null**: `null`

## 🧪 Testing

### Ejecutar Tests Unitarios

```bash
# Todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Por proyecto específico
dotnet test tests/RuleEngineCLI.Domain.Tests
```

### Áreas de Testing Clave

1. **Domain Tests**
   - Value Objects: validación y igualdad
   - Entities: lógica de negocio
   - ValidationReport: agregación de resultados

2. **Application Tests**
   - EvaluateRulesUseCase con mocks
   - Manejo de errores
   - Flujo completo

3. **Infrastructure Tests**
   - JsonRuleRepository: carga desde archivo
   - ExpressionEvaluator: diferentes expresiones
   - Mappers: conversión de modelos

## 🎓 Decisiones de Diseño

### 1. Clean Architecture
**Por qué**: Separación clara de responsabilidades, testabilidad máxima, independencia de frameworks.

### 2. Value Objects Inmutables
**Por qué**: Garantiza consistencia del dominio, thread-safe, facilita reasoning sobre el código.

### 3. Repository Pattern
**Por qué**: Abstrae la persistencia, permite cambiar fácilmente de JSON a DB sin tocar el dominio.

### 4. Strategy Pattern (ExpressionEvaluator)
**Por qué**: Permite agregar nuevos tipos de evaluadores (regex, custom logic) sin modificar código existente.

### 5. Aggregate Root (ValidationReport)
**Por qué**: Encapsula la lógica de negocio de cálculo de estado final, mantiene consistencia.

### 6. Factory Methods
**Por qué**: Centraliza validaciones de creación, garantiza objetos de dominio válidos.

### 7. Dependency Injection Manual
**Por qué**: Control total sobre el grafo de dependencias, educativo, sin magia de frameworks.

## 🔄 Extensibilidad Futura

### Nuevos Tipos de Reglas
```csharp
public class RegexExpressionEvaluator : IExpressionEvaluator
{
    public bool CanEvaluate(Rule rule) => rule.Expression.Value.StartsWith("regex:");
    // Implementación...
}
```

### Nuevas Fuentes de Reglas
```csharp
public class DatabaseRuleRepository : IRuleRepository
{
    // Cargar desde SQL Server, PostgreSQL, etc.
}
```

### Nuevos Formatos de Output
```csharp
public class JsonReportFormatter
{
    public string FormatAsJson(ValidationReportDto report) { }
}
```

## 📊 Ejemplos de Ejecución

### Caso: Validación Exitosa

```bash
$ ruleengine -r rules.json -i valid-input.json

╔═══════════════════════════════════════════════════════════════╗
║              RULE ENGINE CLI v1.0                             ║
╚═══════════════════════════════════════════════════════════════╝

Generated At:        2026-02-02 10:30:00 UTC
Total Rules:         7
Rules Passed:        7
Rules Failed:        0
Max Severity Found:  INFO

Final Status:        [PASS]

Exit Code: 0
```

### Caso: Validación con Errores

```bash
$ ruleengine -r rules.json -i invalid-input.json -v

FAILED RULES
───────────────────────────────────────────────────────────────

  [RULE_001] [ERROR]
    Description: User age must be 18 or older
    Message:     User must be at least 18 years old to register.

  [RULE_002] [ERROR]
    Description: Start date must be before end date
    Message:     Start date must be earlier than end date.

Final Status:        [FAIL]

Exit Code: 2
```

## 📝 Para Entrevistas Técnicas

### Preguntas que Este Proyecto Responde

1. **¿Cómo implementas Clean Architecture?**
   - Muestra separación estricta de capas, direccionalidad de dependencias.

2. **¿Qué es DDD y cómo lo aplicas?**
   - Value Objects, Entities, Aggregate Roots, Ubiquitous Language.

3. **¿Conoces SOLID?**
   - Cada principio aplicado con ejemplos concretos en el código.

4. **¿Cómo diseñas para testabilidad?**
   - Inyección de dependencias, interfaces, código sin side effects.

5. **¿Patrón Repository vs Direct Data Access?**
   - Abstracción de persistencia, cambio de fuente de datos sin impacto.

## 🤝 Contribución

Este es un proyecto educativo diseñado para demostrar arquitectura profesional de software. Las contribuciones son bienvenidas:

1. Fork el proyecto
2. Crea una rama feature (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

MIT License - ver archivo LICENSE para detalles.

## 👨‍💻 Autor

Proyecto desarrollado como ejemplo de arquitectura limpia y diseño orientado a dominio para portfolio de desarrollador Senior.

---

**Tags**: Clean Architecture, DDD, SOLID, C#, .NET 8, CLI, Design Patterns, Enterprise Architecture
