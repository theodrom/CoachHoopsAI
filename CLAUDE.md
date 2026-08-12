# CLAUDE.md

Repository-specific instructions for AI-assisted development on CoachHoopsAI.

## Current baseline

- Milestone 0 (repository stabilization + automated test foundation) is
  complete. Tag: `milestone-0`.
- Milestone 1 (game format/timing model + raw-count `TeamStats`, plus a
  follow-up persistence fix) is complete. Tags: `milestone-1`,
  `milestone-1-persistence-fix` (HEAD).
- `dotnet build CoachHoopsAI.sln` succeeds with no errors. `dotnet test
  CoachHoopsAI.sln` passes: 127 tests, 0 failed (36 + 14 + 9 + 68 across the
  four test projects below). Treat this count as a snapshot, not a target -
  re-run rather than trusting this number as it ages.
- Four test projects, no mocking framework, hand-written fakes only:
  - `CoachHoopsAI.Domain.Tests` - `StatRulesEngine` rules/boundaries, `GameFormat`/`GameTiming`.
  - `CoachHoopsAI.Application.Tests` - `GameAnalysisService`/`AnalysisHistoryService` orchestration and persisted-record building, via fakes for the rules engine, LLM client, profile provider, and repository.
  - `CoachHoopsAI.Api.Tests` - request/response mapping (`AnalyzeGameMappings`) and FluentValidation validators.
  - `CoachHoopsAI.Infrastructure.Tests` - the internal-identifier leak filter and `ModelName` on `OpenAiSuggestionClientHttp`, against a faked HTTP transport - **no real OpenAI calls**.
  - See `Docs/09-testing.md` and each project's own README for detail; do not duplicate that detail here.

## Before making changes

1. Read the root [README.md](README.md) first.
2. Read [Docs/README.md](Docs/README.md) for the documentation index.
3. Read the documentation under `Docs/` relevant to the subsystem being changed.
4. Read the README of the project being changed (`CoachHoopsAI.<Project>/README.md`).

## Source of truth

Treat the current code, together with the documentation under `Docs/` and the
project READMEs, as the source of truth for implemented behavior. Historical
milestone documents (`Docs/v1-app-basics.md`, `Docs/v2-ai-integration(current).md`,
`Docs/v3-admin-ui.md`) describe what shipped at a point in time - do not treat
them as descriptions of current architecture, and do not rewrite them to look
like they always described today's system.

## Milestone 1 — implemented architecture

- `TeamStats` (`CoachHoopsAI.Domain.Entities`) is the canonical raw-count
  model - 14 fields, no percentages: `Points`, `FieldGoalsMade`,
  `FieldGoalsAttempted`, `ThreePointsMade`, `ThreePointsAttempted`,
  `FreeThrowsMade`, `FreeThrowsAttempted`, `OffensiveRebounds`,
  `DefensiveRebounds`, `Assists`, `Turnovers`, `Steals`, `Blocks`,
  `PersonalFouls`.
- `GameFormat` (`CoachHoopsAI.Domain.GameContext`) describes a game's
  structure: `RegulationPeriods`, `RegulationPeriodMinutes`,
  `OvertimePeriodMinutes`, plus a `Name` that is descriptive-only and must
  never drive logic.
- `GameTiming` describes where a game currently stands: `CurrentPeriod`,
  `ClockRemaining`. Overtime is **sequential and derived, never a separate
  flag**: periods `1..RegulationPeriods` are regulation, `RegulationPeriods+1,
  +2, ...` are OT1, OT2, .... `IsOvertime`/`OvertimeNumber` are pure functions
  of `CurrentPeriod` vs `RegulationPeriods`, so they can never contradict it.
- `ElapsedGameTime(format)` = completed periods at full length + the elapsed
  portion of the current period (using regulation or OT period length
  depending on where `CurrentPeriod` falls). `RegulationProgress(format)` =
  elapsed regulation time / regulation duration, clamped to `[0,1]`, and
  hardcoded to `1.0` once overtime starts - there is no invented "overtime
  progress" concept.
- `GameFormat` is independent of competition `Level` (`EasyBasket`/`Youth`/
  `Amateur`/`Pro`) - **never infer one from the other**. See
  `Docs/ADR-0004-game-format-timing-and-raw-stats.md`.
- API: `AnalyzeGameRequest` requires `GameFormat`/`GameTiming` on every
  request (`GameFormatDtoValidator`, `GameTimingDtoValidator`, plus a
  request-level cross-field rule rejecting a clock value that exceeds the
  applicable period length). Admin's New Analysis form captures the same
  fields with sensible defaults (4x10, period 1, 10:00 remaining). See
  `Docs/02-api-contracts.md` and `Docs/06-validation.md`.

## Compatibility boundary - do not disturb without a milestone decision

- The rules engine (`StatRulesEngine`) and diagnostics
  (`GameDiagnosticsCalculator`) still run on **legacy percentage-based
  logic**, computed on the fly from raw `TeamStats` counts via
  `LegacyPercentageBridge` (`CoachHoopsAI.Domain.Compatibility`) - two ratio
  functions (field-goal %, three-point %), nothing more.
- `LegacyPercentageBridge` is intentional, temporary Milestone 1 scaffolding.
  **Do not expand it into the new calculated-metrics architecture, and do not
  remove or redesign it** until the later rules/findings milestone (M3)
  replaces what the rules engine consumes.
- `GameFormat`/`GameTiming` are captured and persisted but are **intentionally
  not yet consumed** by the rules engine, diagnostics, or the LLM prompt.

## Milestone roadmap

- **M2** - calculated numerical basketball metrics (facts derived from raw stats).
- **M3** - interpretation/findings/rules. Judgments and thresholds belong here, not M2.
- **M4** - sessions/snapshots.
- **M5** - LLM/Admin integration built on the above.

## Agreed Milestone 2 direction

- M2 ships in small slices, not as one large change.
- **M2A (next task)**: deterministic core metrics only.
- **M2B**: possession/opponent-dependent metrics.
- Live-sample confidence is a separate concern, handled after M2A/M2B.
- Use sensible default basketball formulas that can evolve later - do not
  overengineer configurability now (no premature profile/threshold system for
  metric formulas).
- Calculated metrics are a new, separate layer: do not fold them into raw
  `TeamStats`, legacy `GameDiagnostics`, or the rules engine.

## Decisions that must survive a fresh conversation

- Do not prematurely wire M2 calculations into rules, diagnostics, the LLM
  prompt, Admin, persistence, or API responses - that integration is later
  milestones' work.
- Do not introduce basketball judgments or thresholds while implementing
  numerical metrics (M2 is facts; M3 is judgment).
- Do not round internally - compute at full precision; presentation/display
  owns formatting and rounding.
- Preserve backward compatibility unless a milestone explicitly changes it.
  (The Milestone 1 raw-stat contract break was a one-time, explicitly-approved
  exception for a local-development app with no external consumers - it is
  not a standing policy to skip compatibility.)

## Architecture rules

- Preserve the Clean Architecture layer boundaries (API -> Application ->
  Domain; Infrastructure and Persistence implement Application interfaces).
  See `Docs/01-architecture.md`.

## Workflow

- Run the relevant test project(s) after code changes (`dotnet test
  CoachHoopsAI.<Project>.Tests`, or `dotnet test CoachHoopsAI.sln` for
  everything) - see `Docs/09-testing.md`.
- When a change materially affects behavior or a contract (API request/response
  shape, validation rules, domain model), update the relevant documentation in
  the same change.
