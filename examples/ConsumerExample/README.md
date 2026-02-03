# Consumer Example - Cómo usar RuleEngineCLI como Librería

Este proyecto demuestra cómo consumir **RuleEngineCLI** como una librería .NET desde otro proyecto.

## 🎯 Objetivo

Mostrar la integración de RuleEngineCLI en una aplicación .NET usando:
- Dependency Injection (Microsoft.Extensions.DependencyInjection)
- Referencias de proyecto
- Configuración programática
- Validación de objetos de dominio

## 🏗️ Estructura

```
ConsumerExample/
├── Program.cs                          # Ejemplo de uso
├── RuleEngineCLI.ConsumerExample.csproj # Referencias a RuleEngineCLI
└── README.md                           # Esta documentación
```

## 🚀 Ejecutar el Ejemplo

```bash
# Desde la raíz del proyecto RuleEngineCLI
cd examples/ConsumerExample
dotnet run
```

## 📋 Qué Hace el Ejemplo

1. **Configura Dependency Injection**: 
   - Registra IRuleEngine, IRuleRepository, IExpressionEvaluator, ILogger

2. **Ejemplo 1 - Usuario Válido**:
   - Valida datos que cumplen todas las reglas
   - Resultado: ✅ PASS

3. **Ejemplo 2 - Usuario Inválido**:
   - Valida datos que fallan múltiples reglas (edad, balance, fechas)
   - Resultado: ❌ FAIL con detalles de errores

4. **Ejemplo 3 - Placeholder**:
   - Muestra cómo podrías extender con validaciones personalizadas

## 🔑 Conceptos Clave Demostrados

### 1. Referencias de Proyecto

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\RuleEngineCLI.Domain\RuleEngineCLI.Domain.csproj" />
  <ProjectReference Include="..\..\src\RuleEngineCLI.Application\RuleEngineCLI.Application.csproj" />
  <ProjectReference Include="..\..\src\RuleEngineCLI.Infrastructure\RuleEngineCLI.Infrastructure.csproj" />
</ItemGroup>
```

### 2. Configuración de Servicios

```csharp
var services = new ServiceCollection();
services.AddSingleton<ILogger>(new ConsoleLogger());
services.AddSingleton<IRuleRepository>(new JsonRuleRepository("rules.json"));
services.AddSingleton<IExpressionEvaluator, ComparisonExpressionEvaluator>();
services.AddSingleton<IRuleEngine, RuleEngine>();
```

### 3. Conversión de Objetos a DTO

```csharp
var inputData = new ValidationInputDto(new Dictionary<string, object?>
{
    { "age", user.Age },
    { "balance", user.Balance },
    { "username", user.Username }
});
```

### 4. Ejecución de Validación

```csharp
var report = await ruleEngine.EvaluateEnabledRulesAsync(inputData);

if (report.Status == "FAIL")
{
    // Manejar errores
    foreach (var error in report.Results.Where(r => !r.Passed))
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
```

## 💡 Casos de Uso Reales

### Validación en API

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] UserDto dto)
{
    var input = new ValidationInputDto(/* mapear dto */);
    var report = await _ruleEngine.EvaluateEnabledRulesAsync(input);
    
    if (report.Status == "FAIL")
        return BadRequest(report.Results.Where(r => !r.Passed));
        
    // Continuar con el registro
    return Ok();
}
```

### Validación en Servicios

```csharp
public class UserService
{
    private readonly IRuleEngine _ruleEngine;
    
    public async Task<bool> ValidateUserData(User user)
    {
        var input = MapToValidationInput(user);
        var report = await _ruleEngine.EvaluateEnabledRulesAsync(input);
        return report.Status == "PASS";
    }
}
```

### Validación Batch

```csharp
public async Task<List<ValidationResult>> ValidateBatch(List<User> users)
{
    var results = new List<ValidationResult>();
    
    foreach (var user in users)
    {
        var input = MapToValidationInput(user);
        var report = await _ruleEngine.EvaluateEnabledRulesAsync(input);
        results.Add(new ValidationResult(user.Id, report));
    }
    
    return results;
}
```

## 🎨 Personalización

### Usar Tu Propio Logger

```csharp
public class MyCustomLogger : ILogger
{
    public void LogInformation(string message) 
    {
        // Tu lógica de logging
    }
    // ... otros métodos
}

// En la configuración
services.AddSingleton<ILogger>(new MyCustomLogger());
```

### Cargar Reglas desde Base de Datos

```csharp
public class DatabaseRuleRepository : IRuleRepository
{
    private readonly DbContext _context;
    
    public async Task<IEnumerable<Rule>> LoadAllRulesAsync(CancellationToken ct)
    {
        var rulesFromDb = await _context.Rules.ToListAsync(ct);
        return rulesFromDb.Select(MapToRule);
    }
}
```

## 📊 Output Esperado

```
=== RuleEngineCLI - Consumer Example ===

📋 Ejemplo 1: Validando usuario válido...

Estado de validación: [PASS]
Total de reglas evaluadas: 7
Reglas que pasaron: 7
Reglas que fallaron: 0
Severidad máxima: INFO

✅ Todos los datos son válidos!

------------------------------------------------------------

📋 Ejemplo 2: Validando usuario inválido...

Estado de validación: [FAIL]
Total de reglas evaluadas: 7
Reglas que pasaron: 1
Reglas que fallaron: 6
Severidad máxima: ERROR

❌ Errores encontrados:
  [RULE_001] User must be at least 18 years old to register.
  [RULE_003] Account balance cannot be negative.
  [RULE_004] Username is required and cannot be null.
  ...
```

## 🔧 Troubleshooting

### Error: "File not found: rules.json"
- Asegúrate de ejecutar desde el directorio `examples/ConsumerExample`
- O ajusta la ruta en `ConfigureServices()`:
  ```csharp
  var rulesPath = Path.Combine("../../examples/rules.json");
  ```

### Error: "Could not load file or assembly"
- Ejecuta `dotnet restore` en la raíz del proyecto
- Verifica que las referencias de proyecto sean correctas

## 📚 Recursos Adicionales

- [Documentación Principal](../../README.md)
- [Ejemplos de Reglas](../rules.json)
- [Tests Unitarios](../../tests/)

## 🤝 Contribuir

Si tienes ideas para mejorar este ejemplo o agregar nuevos casos de uso, ¡son bienvenidas!
