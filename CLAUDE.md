# CLAUDE.md

Repository-specific instructions for AI-assisted development on CoachHoopsAI.

## Current baseline

- Milestone 0 (repository stabilization + automated test foundation) is
  complete. Tag: `milestone-0`.
- Milestone 1 (game format/timing model + raw-count `TeamStats`, plus a
  follow-up persistence fix) is complete. Tags: `milestone-1`,
  `milestone-1-persistence-fix`.
- Milestone 2 (calculated numerical basketball metrics - M2A team metrics,
  M2B possession/cross-team metrics, M2C live estimated pace) is complete on
  `main`. Not yet tagged.
- Verified 2026-09-18: `dotnet build CoachHoopsAI.sln` succeeds with no
  errors; `dotnet test CoachHoopsAI.sln` passes 159 tests, 0 failed
  (68 + 14 + 9 + 68 across the four test projects below). Treat this as a
  dated snapshot, not a permanent expected count - re-run rather than
  trusting this number as it ages.
- Four test projects, no mocking framework, hand-written fakes only:
  - `CoachHoopsAI.Domain.Tests` - `StatRulesEngine` rules/boundaries,
    `GameFormat`/`GameTiming`, and the M2 calculated-metrics calculators
    (`CalculatedMetricsCalculator`, `GameCalculatedMetricsCalculator`).
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

## Milestone 2 — implemented architecture

A new, separate calculated-metrics layer under `CoachHoopsAI.Domain.Metrics` -
facts derived from raw stats, never rounded internally, never folded into
`TeamStats`, legacy `GameDiagnostics`, or the rules engine.

- **M2A** - `CalculatedMetricsCalculator.Calculate(TeamStats)` produces
  `TeamCalculatedMetrics` from one side's raw counts alone: FG%/3P%/FT%,
  total rebounds, eFG%, assist-to-turnover ratio (`null` when turnovers are
  zero, never forced to `0`), 3PA rate, FT rate.
- **M2B** - `GameCalculatedMetricsCalculator.Calculate(TeamStats team,
  TeamStats opponent)` reuses M2A for each side, then adds possession- and
  opponent-dependent fields to `TeamCalculatedMetrics`: `EstimatedPossessions`
  (`FGA - OREB + TO + 0.44*FTA`), offensive rating, turnover rate, OREB%/
  DREB%, steal rate, foul rate - all `null` when the relevant denominator is
  zero. Team and Opponent are calculated symmetrically (swapping the two
  arguments swaps the two outputs).
- **M2C** - an overload, `Calculate(TeamStats team, TeamStats opponent,
  GameFormat format, GameTiming timing)`, adds two game-level fields to
  `GameCalculatedMetrics`: `GameEstimatedPossessions` (mean of the two
  sides' `EstimatedPossessions`) and nullable `EstimatedPace`
  (`GameEstimatedPossessions * RegulationDuration / ElapsedGameTime(format)`,
  `null` when elapsed time is zero). Pace normalizes against
  `GameFormat.RegulationDuration` even during overtime (`ElapsedGameTime`
  already includes elapsed OT) - there is no separate "overtime pace". The
  original two-argument overload is unchanged and simply cannot produce
  `EstimatedPace` (no dummy timing is substituted to force a value).
- Full formulas and null-semantics tables: `Docs/03-domain-and-rules.md`.
- The first M3 rule (`LowEffectiveFieldGoalPercentage`, see below) now reads
  M2A's `EffectiveFieldGoalPercentage` directly. Everything else in M2A/B/C
  is still not wired into diagnostics, the LLM prompt, Admin, persistence, or
  API responses - the rest of that integration is later M3 work.

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
- The M2 calculated-metrics layer (`TeamCalculatedMetrics`/
  `GameCalculatedMetrics`) exists alongside `LegacyPercentageBridge`, not in
  place of it. `StatRulesEngine` still reads the bridge's two ratios for its
  original (M1) rules; one new M3 rule (`LowEffectiveFieldGoalPercentage`)
  additionally reads `CalculatedMetricsCalculator`'s eFG% - that is the only
  rule migrated off the bridge so far, not a signal to migrate the rest yet.
- `GameFormat`/`GameTiming` are captured and persisted but are **intentionally
  not yet consumed** by the rules engine, diagnostics, or the LLM prompt.

## Milestone roadmap

- **M2** - calculated numerical basketball metrics (facts derived from raw
  stats). Complete: M2A, M2B, M2C.
- **M3 (in progress)** - interpretation/findings/rules grounded in M2's
  calculated metrics. Judgments and thresholds belong here, not M2. First
  slice landed: `LowEffectiveFieldGoalPercentage`.
- **M4** - sessions/snapshots.
- **M5** - LLM/Admin integration built on the above.

## Next steps (M3)

First slice complete: `LowEffectiveFieldGoalPercentage` (`StatRulesEngine`)
flags a low team effective field-goal % (M2A), gated on a minimum
field-goal-attempts sample size so a small/zero sample can't read as
"inefficient." See `Docs/03-domain-and-rules.md`'s "Findings (Milestone 3)"
section for the full reuse-vs-new-tag reasoning and threshold rationale.

**`ProblemTag` additions must always be appended, never inserted.**
`AnalysisRecord.ProblemTagsJson` persists tags as a raw integer array
(`System.Text.Json`'s default enum encoding, read back by
`ProblemTagDto.MapTag(int)` in `CoachHoopsAI.Admin`) - inserting a new member
earlier in the enum reassigns the ordinals, and therefore the meaning, of
every tag after it in already-persisted analysis records. (The API's own
`AnalyzeGameResponse.ProblemTags` uses `.ToString()` instead and is
unaffected by ordinal position - only the persisted history path is ordinal-
sensitive.)

Further M3 work should keep introducing findings grounded in M2's calculated
metrics (`TeamCalculatedMetrics`/`GameCalculatedMetrics`), not on the legacy
`LegacyPercentageBridge` percentages most of `StatRulesEngine` still uses.

Before adding new findings, review the existing `ProblemTag` set
(`CoachHoopsAI.Domain.Enums`) against what the current box-score model can
actually establish. Several existing tags already claim causes the raw stats
don't fully support - `RulesProfile` itself already labels some of the
thresholds behind them as proxies (e.g. the fields behind
`InteriorDefenseProblem` and `TransitionDefenseProblem`); `LackOfPaintPressure`
has a similar gap, since `TeamStats` has no shot-location data. In
particular: **`EstimatedPace` (M2C) measures tempo, but does not by itself
establish `PaceControlProblem`** - turning a tempo number into a "pace is a
problem" judgment needs a threshold/interpretation layer on top, which is M3
scope, not something M2 already did.

Two input edge cases surfaced during the M2 review are open questions, not
confirmed defects or completed fixes - worth a decision before M3 relies on
these values:

- Box-score input that passes today's API validation can still produce a
  negative `EstimatedPossessions` (no validation rule relates
  `OffensiveRebounds` to `FieldGoalsAttempted`/`Turnovers`/`FreeThrowsAttempted`).
- `GameTiming.ElapsedGameTime` has no defensive clamp for a clock value that
  exceeds its period length; the API blocks this via a cross-field validator,
  but a Domain/Application caller that builds `GameTiming` directly is not
  protected the same way.

Live-sample confidence (how much a metric like `EstimatedPace` should be
trusted early in a game, before enough of it has been played) is also still
an open, unaddressed concern - not resolved by M2A/M2B/M2C.

## Decisions that must survive a fresh conversation

- M2's calculated metrics (complete: M2A/M2B/M2C) are not wired into rules,
  diagnostics, the LLM prompt, Admin, persistence, or API responses - wiring
  them in is M3+ work, not a leftover M2 task.
- Do not introduce basketball judgments or thresholds while implementing
  numerical metrics (M2 is facts; M3 is judgment). This applies to any future
  calculated-metric work, not just the completed M2A/B/C.
- Do not round internally - compute at full precision; presentation/display
  owns formatting and rounding.
- Preserve backward compatibility unless a milestone explicitly changes it.
  (The Milestone 1 raw-stat contract break was a one-time, explicitly-approved
  exception for a local-development app with no external consumers - it is
  not a standing policy to skip compatibility.)
- Prefer sensible default formulas over a formula-configuration system for
  new calculated metrics (this guided M2A/B/C). `RulesProfile`'s threshold
  configurability is a separate, already-existing pattern meant for M3's
  judgments - not a reason to add configurability to metric formulas
  themselves.

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
