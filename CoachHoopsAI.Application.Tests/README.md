# CoachHoopsAI.Application.Tests

## Purpose

Orchestration tests for `CoachHoopsAI.Application` - primarily `GameAnalysisService`,
plus `GameDiagnosticsCalculator`.

## What's covered

- `GameAnalysisService.AnalyzeAsync`: that it passes team/opponent stats and the
  resolved `RulesProfile` to the rules engine, passes `Level`/override to the
  profile provider, passes the engine's tags and computed diagnostics into the LLM
  client, assembles the final `GameAnalysisResult` from all three dependencies'
  outputs, and still calls the LLM client (with an empty tag collection) when no
  rules fire. These are wiring tests - rule thresholds themselves are covered in
  `CoachHoopsAI.Domain.Tests`.
- `GameDiagnosticsCalculator.Calculate`: diff/percentage arithmetic from raw
  `TeamStats` counts, including a zero-attempts case (no divide-by-zero).

## Testing boundary

No real HTTP calls, no real OpenAI calls, no database. `IStatRulesEngine`,
`ILlmSuggestionClient`, and `IRulesProfileProvider` are replaced with hand-written
fakes; `GameDiagnosticsCalculator` is exercised directly since it's a pure
static method.

## Fakes/helpers

All in `TestDoubles/` - hand-written, no mocking framework:

- `FakeStatRulesEngine` - records the team/opponent/profile it was called with; returns a configured tag list
- `FakeLlmSuggestionClient` - records what it was grounded with; returns configured suggestions
- `FakeRulesProfileProvider` - records the level/override it was called with; returns a configured `ResolvedRulesProfile`

## Running

```
dotnet test CoachHoopsAI.Application.Tests
```
