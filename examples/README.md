# RuleEngineCLI - Ejemplos de Uso

Este directorio contiene ejemplos de archivos de configuración y datos para probar la aplicación.

## Archivos Incluidos

### 1. rules.json
Archivo de reglas de ejemplo que incluye:
- 7 reglas habilitadas
- 1 regla deshabilitada
- Diferentes severidades (INFO, WARN, ERROR)
- Expresiones simples y compuestas (con && y ||)

### 2. valid-input.json
Datos de entrada que pasan todas las reglas habilitadas.
- Age: 25 (>= 18) ✅
- Start/End dates: válidas ✅
- Balance: positivo ✅
- Username: no null ✅
- Todo cumple

### 3. invalid-input.json
Datos de entrada que fallan múltiples reglas con severidad ERROR.
- Age: 16 (< 18) ❌
- Start date > End date ❌
- Balance negativo ❌
- Username null ❌

### 4. warning-input.json
Datos de entrada que generan warnings pero no errors.
- Reglas ERROR: pasan ✅
- Email domain: no corporativo (WARNING) ⚠️
- Transaction amount: excede límite (WARNING) ⚠️

## Comandos de Prueba

### Prueba 1: Validación exitosa (verbose)
```bash
dotnet run --project ../src/RuleEngineCLI.Presentation.CLI -- \
  --rules rules.json \
  --input valid-input.json \
  --verbose
```
**Resultado esperado**: Status PASS, exit code 0

### Prueba 2: Validación con errores
```bash
dotnet run --project ../src/RuleEngineCLI.Presentation.CLI -- \
  --rules rules.json \
  --input invalid-input.json
```
**Resultado esperado**: Status FAIL, exit code 2

### Prueba 3: Validación con warnings
```bash
dotnet run --project ../src/RuleEngineCLI.Presentation.CLI -- \
  --rules rules.json \
  --input warning-input.json \
  --verbose
```
**Resultado esperado**: Status WARNING, exit code 1

### Prueba 4: Datos inline
```bash
dotnet run --project ../src/RuleEngineCLI.Presentation.CLI -- \
  --rules rules.json \
  --data '{"age": 20, "balance": 100, "username": "test", "startDate": "2026-01-01", "endDate": "2026-12-31", "emailDomain": "company.com", "transactionAmount": 5000, "isActive": true, "isVerified": true}'
```

### Prueba 5: Modo debug
```bash
dotnet run --project ../src/RuleEngineCLI.Presentation.CLI -- \
  --rules rules.json \
  --input invalid-input.json \
  --debug
```

## Crear Tus Propias Reglas

Puedes crear tu propio archivo de reglas siguiendo esta estructura:

```json
{
  "version": "1.0.0",
  "rules": [
    {
      "id": "CUSTOM_RULE",
      "description": "Tu descripción",
      "expression": "propertyName >= 100",
      "severity": "ERROR",
      "errorMessage": "Mensaje de error personalizado",
      "isEnabled": true
    }
  ]
}
```

## Expresiones Avanzadas

### Comparaciones Simples
```json
"expression": "age >= 18"
"expression": "status == \"active\""
"expression": "balance > 0"
```

### Operadores Lógicos
```json
"expression": "age >= 18 && age <= 65"
"expression": "role == \"admin\" || role == \"superadmin\""
"expression": "isActive == true && balance > 0"
```

### Comparación de Fechas
```json
"expression": "startDate < endDate"
"expression": "expiryDate >= \"2026-01-01\""
```

### Null Checks
```json
"expression": "username != null"
"expression": "email != null"
```
