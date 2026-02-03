using Microsoft.Extensions.Logging;
using RuleEngineAppLogger = RuleEngineCLI.Application.Services.ILogger;
using RuleEngineCLI.Application.Implementation;
using RuleEngineCLI.Application.Services;
using RuleEngineCLI.Domain.Repositories;
using RuleEngineCLI.Infrastructure.Evaluation;
using RuleEngineCLI.Infrastructure.Logging;
using RuleEngineCLI.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register RuleEngineCLI services
builder.Services.AddScoped<IRuleRepository>(sp => new JsonRuleRepository("path/to/rules.json")); // This will be overridden per request
builder.Services.AddScoped<IExpressionEvaluator, ComparisonExpressionEvaluator>();
builder.Services.AddScoped<RuleEngineAppLogger, ConsoleLogger>();
builder.Services.AddScoped<IRuleEngine, RuleEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();