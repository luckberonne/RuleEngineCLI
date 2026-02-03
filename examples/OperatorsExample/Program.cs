using RuleEngineCLI.Application.DTOs;
using RuleEngineCLI.Infrastructure.Evaluation;

namespace RuleEngineCLI.OperatorsExample;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  RuleEngineCLI - Advanced Operators Example (Phase 3)     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var evaluator = new AdvancedOperatorsEvaluator();

        DemoRegEx(evaluator);
        DemoInNotIn(evaluator);
        DemoBetween(evaluator);
        DemoIsNull(evaluator);
        DemoStringOperators(evaluator);
        DemoCombined(evaluator);
    }

    private static void DemoRegEx(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("📧 Demo 1: RegEx Operator - Email Validation");
        Console.WriteLine();

        var rule = CreateRule("email RegEx ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$");

        var validEmail = new ValidationInputDto();
        validEmail.Properties["email"] = "user@example.com";

        var invalidEmail = new ValidationInputDto();
        invalidEmail.Properties["email"] = "invalid-email";

        Console.WriteLine($"Rule: {rule.Expression.Value}");
        Console.WriteLine();
        Console.WriteLine($"✅ Valid email 'user@example.com': {evaluator.Evaluate(rule, validEmail)}");
        Console.WriteLine($"❌ Invalid email 'invalid-email': {evaluator.Evaluate(rule, invalidEmail)}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoInNotIn(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("🌍 Demo 2: In/NotIn Operators");
        Console.WriteLine();

        var ruleIn = CreateRule("country In [Argentina, Brazil, Chile]");
        var ruleNotIn = CreateRule("status NotIn [banned, suspended]");

        var validCountry = new ValidationInputDto();
        validCountry.Properties["country"] = "Argentina";

        var invalidCountry = new ValidationInputDto();
        invalidCountry.Properties["country"] = "USA";

        Console.WriteLine($"Rule In: {ruleIn.Expression.Value}");
        Console.WriteLine($"✅ Country 'Argentina': {evaluator.Evaluate(ruleIn, validCountry)}");
        Console.WriteLine($"❌ Country 'USA': {evaluator.Evaluate(ruleIn, invalidCountry)}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoBetween(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("🎂 Demo 3: Between Operator - Age Range");
        Console.WriteLine();

        var rule = CreateRule("age Between 18 And 65");

        var validAge = new ValidationInputDto();
        validAge.Properties["age"] = 25;

        var tooYoung = new ValidationInputDto();
        tooYoung.Properties["age"] = 15;

        Console.WriteLine($"Rule: {rule.Expression.Value}");
        Console.WriteLine();
        Console.WriteLine($"✅ Age 25: {evaluator.Evaluate(rule, validAge)}");
        Console.WriteLine($"❌ Age 15: {evaluator.Evaluate(rule, tooYoung)}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoIsNull(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("🔍 Demo 4: IsNull/IsNotNull Operators");
        Console.WriteLine();

        var ruleIsNull = CreateRule("middleName IsNull");
        var ruleIsNotNull = CreateRule("email IsNotNull");

        var withMiddleName = new ValidationInputDto();
        withMiddleName.Properties["middleName"] = "Alexander";
        withMiddleName.Properties["email"] = "test@example.com";

        var withoutMiddleName = new ValidationInputDto();
        withoutMiddleName.Properties["email"] = "test@example.com";

        var noEmail = new ValidationInputDto();

        Console.WriteLine($"Rule IsNull: {ruleIsNull.Expression.Value}");
        Console.WriteLine($"❌ With middle name: {evaluator.Evaluate(ruleIsNull, withMiddleName)}");
        Console.WriteLine($"✅ Without middle name: {evaluator.Evaluate(ruleIsNull, withoutMiddleName)}");
        Console.WriteLine();

        Console.WriteLine($"Rule IsNotNull: {ruleIsNotNull.Expression.Value}");
        Console.WriteLine($"✅ With email: {evaluator.Evaluate(ruleIsNotNull, withMiddleName)}");
        Console.WriteLine($"❌ Without email: {evaluator.Evaluate(ruleIsNotNull, noEmail)}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoStringOperators(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("🔤 Demo 5: String Operators");
        Console.WriteLine();

        var ruleStartsWith = CreateRule("username StartsWith admin");
        var ruleEndsWith = CreateRule("email EndsWith @company.com");
        var ruleContains = CreateRule("description Contains urgent");

        var adminUser = new ValidationInputDto();
        adminUser.Properties["username"] = "admin123";

        var companyEmail = new ValidationInputDto();
        companyEmail.Properties["email"] = "john@company.com";

        var urgentDesc = new ValidationInputDto();
        urgentDesc.Properties["description"] = "This is an URGENT request";

        Console.WriteLine($"Rule StartsWith: {ruleStartsWith.Expression.Value}");
        Console.WriteLine($"✅ Username 'admin123': {evaluator.Evaluate(ruleStartsWith, adminUser)}");
        Console.WriteLine();

        Console.WriteLine($"Rule EndsWith: {ruleEndsWith.Expression.Value}");
        Console.WriteLine($"✅ Email 'john@company.com': {evaluator.Evaluate(ruleEndsWith, companyEmail)}");
        Console.WriteLine();

        Console.WriteLine($"Rule Contains: {ruleContains.Expression.Value}");
        Console.WriteLine($"✅ Description with 'URGENT': {evaluator.Evaluate(ruleContains, urgentDesc)}");
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void DemoCombined(AdvancedOperatorsEvaluator evaluator)
    {
        Console.WriteLine("🎯 Demo 6: Real-World Scenario - User Registration");
        Console.WriteLine();

        var validUser = new ValidationInputDto();
        validUser.Properties["email"] = "newuser@example.com";
        validUser.Properties["age"] = 25;
        validUser.Properties["country"] = "Argentina";

        var emailRule = CreateRule("email RegEx ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$");
        var ageRule = CreateRule("age Between 18 And 100");
        var countryRule = CreateRule("country In [Argentina, Brazil, Chile, Uruguay]");

        var emailValid = evaluator.Evaluate(emailRule, validUser);
        var ageValid = evaluator.Evaluate(ageRule, validUser);
        var countryValid = evaluator.Evaluate(countryRule, validUser);

        Console.WriteLine($"Email validation: {(emailValid ? "✅ PASS" : "❌ FAIL")}");
        Console.WriteLine($"Age validation: {(ageValid ? "✅ PASS" : "❌ FAIL")}");
        Console.WriteLine($"Country validation: {(countryValid ? "✅ PASS" : "❌ FAIL")}");
        Console.WriteLine();

        if (emailValid && ageValid && countryValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎉 All validations passed! User can be registered.");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("💡 Advanced operators available:");
        Console.WriteLine("   • RegEx - Pattern matching");
        Console.WriteLine("   • In/NotIn - Membership testing");
        Console.WriteLine("   • Between - Range validation");
        Console.WriteLine("   • IsNull/IsNotNull - Null checking");
        Console.WriteLine("   • StartsWith/EndsWith/Contains - String operations");
    }

    private static RuleEngineCLI.Domain.Entities.Rule CreateRule(string expression)
    {
        return RuleEngineCLI.Domain.Entities.Rule.Create(
            RuleEngineCLI.Domain.ValueObjects.RuleId.Create("DEMO"),
            "Demo Rule",
            RuleEngineCLI.Domain.ValueObjects.Expression.Create(expression),
            RuleEngineCLI.Domain.ValueObjects.Severity.Error,
            "Demo validation");
    }
}
