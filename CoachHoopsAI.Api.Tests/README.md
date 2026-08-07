# CoachHoopsAI.Api.Tests

## Purpose

Regression tests for `CoachHoopsAI.Api`'s request/response mapping and
FluentValidation validators.

## What's covered

- `AnalyzeGameMappings.ToGameAnalysisInput`: a reflection-driven test that walks
  every `TeamStatsDto` property and asserts a same-named property exists on the
  domain `TeamStats` with an equal value - written to fail if a stat property is
  ever added to one side without the other, which is the exact class of bug that
  broke the build before the test suite existed. Also covers null Team/Opponent
  defaulting, `GameFormat`/`GameTiming` mapping (including the `ClockRemainingSeconds`
  -> `TimeSpan` conversion and null-mapping), `Level` string parsing (all levels,
  case variants, blank/garbage fallback), and metadata/notes mapping.
- `AnalyzeGameMappings.ToResponseDto`: tag-to-string and suggestion
  category-bucketing.
- `TeamStatsDtoValidator`: non-negative counts, made-cannot-exceed-attempted for
  field goals/three-pointers/free throws, three-pointers-are-a-subset-of-field-goals,
  and an all-zero stat line passing (zero is legitimate).
- `AnalyzeGameRequestValidator`: required Team/Opponent/GameFormat/GameTiming,
  nested validation propagation, allowed `Level` values, `GameDate`/`Notes`
  bounds, GameFormat field validation, and the GameTiming cross-field
  clock-vs-period-length rule (including the overtime-vs-regulation branch).

## Testing boundary

No HTTP server is started and no controllers are exercised - validators and the
`AnalyzeGameMappings` extension methods are called directly against their inputs.

## Fakes/helpers

None - validators and mappings are pure, so no test doubles are needed.

## Running

```
dotnet test CoachHoopsAI.Api.Tests
```
