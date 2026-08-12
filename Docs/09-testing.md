# Testing

## Test projects

| Project | Tests what | Boundary |
|---|---|---|
| `CoachHoopsAI.Domain.Tests` | `StatRulesEngine` rules and boundaries, `GameFormat`/`GameTiming`, `CalculatedMetricsCalculator`, `GameCalculatedMetricsCalculator` | Unit - pure domain logic, no fakes needed |
| `CoachHoopsAI.Application.Tests` | `GameAnalysisService` orchestration, `GameDiagnosticsCalculator` | Unit - rules engine, LLM client, and profile provider replaced with hand-written fakes |
| `CoachHoopsAI.Api.Tests` | Request/response mapping (`AnalyzeGameMappings`) and FluentValidation validators | Unit - validators and mappings called directly; no HTTP server, no controllers exercised |
| `CoachHoopsAI.Infrastructure.Tests` | The internal-identifier leak filter in `OpenAiSuggestionClientHttp` | Boundary - exercises the real client's public method against a fake `HttpMessageHandler`; **does not call the real OpenAI API** |

Each project's own README documents what it currently covers in more detail.

## Current total

150 tests across the four projects (59 + 14 + 9 + 68), all passing as of this
writing. This number will drift as tests are added - treat it as a snapshot,
not a target.

## Unit vs. API-boundary vs. infrastructure-boundary

- **Unit** (`Domain.Tests`, `Application.Tests`, most of `Api.Tests`): exercises
  a class or method directly, with any collaborators replaced by hand-written
  fakes where needed. No process boundary is crossed.
- **Infrastructure-boundary** (`Infrastructure.Tests`): exercises a real class
  (`OpenAiSuggestionClientHttp`) through its actual HTTP call path, but with the
  transport (`HttpMessageHandler`) faked out, so the test verifies the client's
  real request-building and response-parsing logic without depending on network
  access or a real API key.
- There is currently no true API-boundary layer (no `WebApplicationFactory` /
  in-memory server tests exercising controllers end-to-end) and no integration
  tests against a real database.

## Regression JSON samples vs. automated tests

`Docs/samples/` predates the automated test projects and serves a different
purpose: manually POSTing a sample payload to a running `CoachHoopsAI.Api` and
comparing the response against documented expectations (see
`Docs/samples/README.md`). It is not run by `dotnet test` and is not a
substitute for the automated suites above - it remains useful for exercising
the full stack (validation, rules engine, persistence, real or fake AI client)
end-to-end in a way the current unit tests don't.

## Running tests

All projects:

```
dotnet test CoachHoopsAI.sln
```

A single project:

```
dotnet test CoachHoopsAI.Domain.Tests
dotnet test CoachHoopsAI.Application.Tests
dotnet test CoachHoopsAI.Api.Tests
dotnet test CoachHoopsAI.Infrastructure.Tests
```
