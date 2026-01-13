using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;
using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Domain.Rules;
using CoachHoopsAI.Infrastructure.AI;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Validators
builder.Services.AddScoped<IValidator<AnalyzeGameRequest>, AnalyzeGameRequestValidator>();
builder.Services.AddScoped<IValidator<TeamStatsDto>, TeamStatsDtoValidator>();

// Domain
builder.Services.AddSingleton<IStatRulesEngine, StatRulesEngine>();

// Application
builder.Services.AddScoped<IGameAnalysisService, CoachHoopsAI.Application.Services.GameAnalysisService>();

builder.Services.Configure<RulesProfilesOptions>(builder.Configuration.GetSection("RulesProfiles"));
builder.Services.AddSingleton<IRulesProfileProvider, RulesProfileProvider>();

//OpenAI Client
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
var aiProvider = builder.Configuration["Ai:Provider"]?.Trim();

// Toggle AI provider
if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
{
    // HttpClient for OpenAI
    builder.Services.AddHttpClient<ILlmSuggestionClient, OpenAiSuggestionClientHttp>();
}
else
{
    // Use fake AI client
    builder.Services.AddScoped<ILlmSuggestionClient, FakeSuggestionClient>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
