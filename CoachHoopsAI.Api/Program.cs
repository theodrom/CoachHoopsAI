using CoachHoopsAI.Api.Contracts;
using CoachHoopsAI.Api.Validators;
using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Domain.Rules;
using CoachHoopsAI.Infrastructure.AI;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IValidator<AnalyzeGameRequest>, AnalyzeGameRequestValidator>();
builder.Services.AddScoped<IValidator<TeamStatsDto>, TeamStatsDtoValidator>();

// Domain
builder.Services.AddSingleton<CoachHoopsAI.Domain.Rules.IStatRulesEngine, CoachHoopsAI.Domain.Rules.StatRulesEngine>();
// Application
builder.Services.AddScoped<IGameAnalysisService, CoachHoopsAI.Application.Services.GameAnalysisService>();
// Infrastructure (fake AI for now)
builder.Services.AddScoped<ILlmSuggestionClient, FakeSuggestionClient>();

builder.Services.Configure<RulesProfilesOptions>(builder.Configuration.GetSection("RulesProfiles"));
builder.Services.AddSingleton<IRulesProfileProvider, RulesProfileProvider>();

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
