# RuleEngineCLI - Credit Risk Scoring Example

Ejemplo completo de **Scoring de Riesgo Crediticio** usando RuleEngineCLI para evaluar la solvencia de solicitantes de crédito.

## 🎯 Objetivo

Demostrar cómo usar RuleEngineCLI para implementar un sistema completo de evaluación de riesgo crediticio que evalúa múltiples factores:

- **Puntaje Crediticio** (FICO Score)
- **Relación Deuda/Ingreso** (DTI Ratio)
- **Estabilidad Laboral**
- **Edad y Experiencia**
- **Pago Inicial** (Down Payment)
- **Valor del Colateral**
- **Historial de Quiebras**

## 🚀 Ejecutar el Ejemplo

```bash
# Desde la raíz del proyecto
cd examples/CreditScoringExample
dotnet run
```

## 📋 Reglas de Evaluación Crediticia

### Archivo: `credit-scoring-rules.json`

El sistema evalúa **12 reglas críticas** organizadas por severidad:

#### **Reglas de ERROR (Críticas - Rechazo Automático)**
- `CREDIT_SCORE_LOW`: Puntaje < 600
- `INCOME_STABILITY`: Ingreso < $3000/mes O Empleo < 2 años
- `DEBT_TO_INCOME_RATIO`: DTI > 43%
- `AGE_REQUIREMENT`: Edad < 18 años
- `DOWN_PAYMENT_MINIMUM`: Pago inicial < 20%
- `COLLATERAL_VALUE`: Colateral insuficiente
- `BANKRUPTCY_CHECK`: Quiebra en últimos 7 años

#### **Reglas de WARNING (Advertencias - Revisión Manual)**
- `CREDIT_SCORE_MEDIUM`: Puntaje 600-749
- `DEBT_TO_INCOME_WARNING`: DTI > 36%
- `PAYMENT_TO_INCOME_RATIO`: Pago mensual > 28% del ingreso

#### **Reglas de INFO (Bonos - Puntaje Positivo)**
- `CREDIT_SCORE_HIGH`: Puntaje ≥ 750
- `AGE_OPTIMAL`: Edad 25-65 años

## 👥 Perfiles de Solicitantes

### **Perfil 1: BAJO RIESGO** (`credit-applicant-good.json`)
```json
{
  "age": 35,
  "creditScore": 780,
  "monthlyIncome": 5500,
  "monthlyDebtPayments": 1200,
  "employmentYears": 8,
  "loanAmount": 250000,
  "downPayment": 50000,
  "collateralValue": 280000
}
```
**Resultado Esperado:** ✅ Todas las reglas pasan - Puntaje ~95/100

### **Perfil 2: RIESGO MODERADO** (`credit-applicant-moderate.json`)
```json
{
  "age": 28,
  "creditScore": 650,
  "monthlyIncome": 4200,
  "monthlyDebtPayments": 1800,
  "employmentYears": 3,
  "loanAmount": 180000,
  "downPayment": 27000,
  "collateralValue": 200000
}
```
**Resultado Esperado:** ⚠️ Algunas warnings - Puntaje ~75/100

### **Perfil 3: ALTO RIESGO** (`credit-applicant-high-risk.json`)
```json
{
  "age": 22,
  "creditScore": 520,
  "monthlyIncome": 2800,
  "monthlyDebtPayments": 1400,
  "employmentYears": 1,
  "loanAmount": 150000,
  "downPayment": 15000,
  "collateralValue": 130000,
  "yearsSinceBankruptcy": 3
}
```
**Resultado Esperado:** ❌ Múltiples errores - Puntaje ~25/100

## 🧮 Sistema de Scoring

### Cálculo del Puntaje
```csharp
int baseScore = 100;

// Penalizaciones por fallos
foreach (var failure in failedRules)
{
    switch (failure.Severity)
    {
        case "ERROR": baseScore -= 25; break;
        case "WARN":  baseScore -= 10; break;
        case "INFO":  baseScore -= 5;  break;
    }
}

// Bonus por reglas pasadas
baseScore += passedRules.Count * 2;

// Rango final: 0-100
return Math.Max(0, Math.Min(100, baseScore));
```

### Interpretación de Puntajes
- **80-100**: Excelente candidato - Aprobación automática
- **60-79**: Buen candidato - Revisión adicional mínima
- **40-59**: Candidato riesgoso - Revisión manual requerida
- **0-39**: Alto riesgo - Probablemente rechazar

## 🏗️ Arquitectura de la Solución

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web API       │    │  Credit Scoring  │    │  Rule Engine    │
│                 │    │   Service        │    │                 │
│ • REST Endpoints│───▶│ • Business Logic │───▶│ • Rule Eval     │
│ • JSON Input    │    │ • Risk Calculation│    │ • Validation    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Database      │    │   Cache/Redis    │    │   Rules JSON    │
│                 │    │                  │    │                 │
│ • Applicant Data│    │ • Rule Cache     │    │ • Business Rules│
│ • Credit History│    │ • Fast Access    │    │ • Scoring Logic │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## 🔧 Personalización

### Agregar Nuevas Reglas
```json
{
  "id": "NEW_RULE",
  "description": "Nueva regla de evaluación",
  "expression": "customField >= threshold",
  "severity": "WARN",
  "errorMessage": "Mensaje personalizado",
  "isEnabled": true
}
```

### Modificar Lógica de Scoring
```csharp
private int CalculateCreditScore(ValidationReportDto result)
{
    // Implementar algoritmo personalizado
    // - Machine Learning models
    // - Statistical models
    // - Expert rules
    // - Hybrid approaches
}
```

## 📊 Casos de Uso Empresariales

### **Banca Minorista**
- Evaluación de solicitudes de crédito personal
- Líneas de crédito rotativas
- Tarjetas de crédito

### **Hipotecas**
- Préstamos para vivienda
- Refinanciamiento
- Construcción de vivienda

### **Comercio**
- Créditos comerciales
- Factoring
- Confirming

### **Fintech**
- Plataformas de préstamos P2P
- Créditos digitales
- Scoring alternativo

## 🎯 Beneficios de Usar RuleEngineCLI

✅ **Configurable**: Reglas en JSON, sin recompilar código
✅ **Escalable**: Evaluación paralela para alto volumen
✅ **Auditable**: Logging completo de todas las decisiones
✅ **Mantenible**: Separación clara de reglas y lógica
✅ **Testable**: Unit tests para cada regla individual
✅ **Integrable**: Fácil integración en sistemas existentes

## 🚀 Próximos Pasos

1. **Integración con Buró de Crédito** - APIs externas para datos crediticios
2. **Machine Learning** - Modelos predictivos para scoring avanzado
3. **Real-time Processing** - Evaluación en tiempo real
4. **Dashboard de Analytics** - Métricas y reportes de riesgo
5. **API REST** - Servicio web para integración con aplicaciones