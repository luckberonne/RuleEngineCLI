# Advanced Operators Example - Phase 3

Demostración completa de los **9 operadores avanzados** agregados en Phase 3 del RuleEngineCLI.

## 🚀 Ejecutar

```bash
dotnet run --project examples/OperatorsExample
```

## 📋 Operadores Demostrados

### 1. **RegEx** - Validación por Expresiones Regulares

Permite validar campos contra patrones regex con protección de timeout (1 segundo).

```csharp
"email RegEx ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
```

**Casos de Uso:**
- Validación de emails
- Formato de números de teléfono
- Validación de códigos postales
- Patrones personalizados

---

### 2. **In** - Pertenencia a Lista

Verifica si un valor está dentro de un conjunto de valores permitidos. Case-insensitive.

```csharp
"country In [Argentina, Brazil, Chile]"
```

**Casos de Uso:**
- Validación de países/regiones permitidas
- Estados válidos de un proceso
- Categorías permitidas

---

### 3. **NotIn** - Exclusión de Lista

Verifica que un valor NO esté en un conjunto de valores prohibidos. Case-insensitive.

```csharp
"status NotIn [banned, suspended, deleted]"
```

**Casos de Uso:**
- Usuarios bloqueados
- Estados prohibidos
- Categorías excluidas

---

### 4. **Between** - Rango Numérico

Valida que un valor numérico esté dentro de un rango (inclusive). Soporta int y double.

```csharp
"age Between 18 And 65"
"salary Between 30000.00 And 150000.00"
```

**Casos de Uso:**
- Validación de edad
- Rangos de precios
- Validación de cantidades

---

### 5. **IsNull** - Verificación de Null

Verifica que un campo NO exista o sea null.

```csharp
"middleName IsNull"
```

**Casos de Uso:**
- Campos opcionales no provistos
- Validación de datos faltantes
- Verificación de campos no requeridos

---

### 6. **IsNotNull** - Verificación de Existencia

Verifica que un campo exista y NO sea null.

```csharp
"email IsNotNull"
```

**Casos de Uso:**
- Campos obligatorios
- Validación de datos requeridos
- Verificación de completitud

---

### 7. **StartsWith** - Prefijo de String

Verifica que un string comience con un valor específico. Case-insensitive.

```csharp
"username StartsWith admin"
```

**Casos de Uso:**
- Validación de nombres de usuario con prefijo
- Códigos que empiezan con cierto valor
- Categorías con prefijos

---

### 8. **EndsWith** - Sufijo de String

Verifica que un string termine con un valor específico. Case-insensitive.

```csharp
"email EndsWith @company.com"
```

**Casos de Uso:**
- Validación de dominios de email
- Extensiones de archivo
- Sufijos requeridos

---

### 9. **Contains** - Substring

Verifica que un string contenga un substring específico. Case-insensitive.

```csharp
"description Contains urgent"
```

**Casos de Uso:**
- Búsqueda de palabras clave
- Validación de contenido
- Filtrado de texto

---

## 📦 Tabla de Referencia Rápida

| Operador | Sintaxis | Tipos | Case-Sensitive | Ejemplo |
|----------|----------|-------|----------------|---------|
| **RegEx** | `field RegEx pattern` | String | Configurable | `email RegEx ^[a-z]+@[a-z]+\\.com$` |
| **In** | `field In [val1, val2, ...]` | String | No | `country In [USA, Canada]` |
| **NotIn** | `field NotIn [val1, val2, ...]` | String | No | `status NotIn [banned]` |
| **Between** | `field Between min And max` | Numeric | N/A | `age Between 18 And 65` |
| **IsNull** | `field IsNull` | Any | N/A | `middleName IsNull` |
| **IsNotNull** | `field IsNotNull` | Any | N/A | `email IsNotNull` |
| **StartsWith** | `field StartsWith value` | String | No | `username StartsWith admin` |
| **EndsWith** | `field EndsWith value` | String | No | `email EndsWith @company.com` |
| **Contains** | `field Contains value` | String | No | `description Contains urgent` |

---

## 🎯 Escenario Real: Validación de Registro de Usuario

El Demo 6 combina múltiples operadores para validar un registro de usuario:

```csharp
// 1. Email válido y de dominio corporativo
"email RegEx ^[a-zA-Z0-9._%+-]+@company\\.com$"

// 2. Edad dentro del rango permitido
"age Between 18 And 65"

// 3. País permitido para el servicio
"country In [Argentina, Brazil, Chile]"
```

Este escenario demuestra cómo combinar operadores para validaciones complejas.

---

## ⚙️ Características Técnicas

### Protección de Timeout (RegEx)
- Timeout de 1 segundo para prevenir ReDoS (Regular Expression Denial of Service)
- Uso de `Regex.IsMatch()` con `TimeSpan.FromSeconds(1)`

### Case-Insensitive por Defecto
- Todos los operadores de string usan `StringComparison.OrdinalIgnoreCase`
- Facilita validaciones flexibles sin preocuparse por mayúsculas

### Conversión Automática de Tipos
- Between convierte automáticamente int → double
- Soporte para tipos numéricos comunes sin conversión manual

### Precedencia de Operadores
El evaluador verifica operadores en el siguiente orden para evitar conflictos:

1. RegEx/Regex/Matches
2. StartsWith/EndsWith/Contains
3. NotIn/In
4. Between
5. IsNotNull/IsNull

Esto previene que "StartsWith" active el operador "In" por contener "In" como substring.

---

## 🔗 Ver También

- [Phase 1 - Cache & Performance](../AdvancedExample/README.md)
- [Phase 2 - Configuration & Validation](../ConfigurationExample/README.md)
- [Documentación Principal](../../README.md)

---

## 📊 Resultados Esperados

Al ejecutar el ejemplo, verás:

```
✅ Demo 1: RegEx - Email válido ✓, email inválido ✗
✅ Demo 2: In/NotIn - Argentina ✓, USA ✗
✅ Demo 3: Between - Age 25 ✓, Age 15 ✗
✅ Demo 4: IsNull/IsNotNull - Con/sin campos ✓
✅ Demo 5: String Operators - Todos ✓
✅ Demo 6: Escenario Real - Registro completo ✓
```

**Total: 59 tests pasando** ✨
