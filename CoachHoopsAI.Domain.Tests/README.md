# CoachHoopsAI.Domain.Tests

## Purpose

Unit tests for `CoachHoopsAI.Domain` - the rules engine and the `GameFormat`/`GameTiming` model.

## What's covered

- `StatRulesEngine.Evaluate`: all 12 current rules, boundary cases (`>=` thresholds
  exactly at/above/below the flag point) for turnover diff, opponent field-goal %,
  fouls diff, and point margin, a "healthy stats trigger nothing" baseline, and a
  multiple-tags-at-once case. Percentage-driven scenarios are built from exact
  made/attempted ratios (e.g. `4/16 = 0.25`), matching how the engine actually
  derives percentages via `LegacyPercentageBridge`.
- `GameFormat.RegulationDuration` across several period/length combinations, and
  that `Name` never affects it.
- `GameTiming.ElapsedGameTime`/`RegulationProgress`/`IsOvertime`/`OvertimeNumber`
  across regulation (start of game, mid-period, halftime, end of regulation) and
  overtime (OT1 start, OT2 underway), plus a defensive clamp check.

## Testing boundary

Pure domain-layer tests - no HTTP, no EF Core, no AI calls, no test doubles or
mocking framework. `CoachHoopsAI.Domain` has no external dependencies to fake at
this layer.

## Fakes/helpers

None - `StatRulesEngine`, `GameFormat`, and `GameTiming` are exercised directly.

## Running

```
dotnet test CoachHoopsAI.Domain.Tests
```
