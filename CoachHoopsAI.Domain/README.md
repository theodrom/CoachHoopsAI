# CoachHoopsAI.Domain

## Purpose

This project contains the core basketball domain logic.

It defines the concepts, language, and deterministic rules used to analyze games and detect problem areas.

---

## Responsibilities

- Define domain entities and value objects
- Define enums representing basketball concepts
- Implement deterministic basketball rules
- Encapsulate rule thresholds via RulesProfile

---

## Key Components

- TeamStats (raw box-score counts, not percentages - see `Docs/03-domain-and-rules.md`)
- GameFormat, GameTiming (a game's structure and current position; see `Docs/03-domain-and-rules.md`)
- Suggestion

### Enums

- Level
- ProblemTag
- SuggestionCategory

### Rules

- IStatRulesEngine
- StatRulesEngine
- RulesProfile

### Compatibility

- LegacyPercentageBridge - temporary scaffolding computing shooting percentages
  from raw counts for `StatRulesEngine`; see `Docs/03-domain-and-rules.md`

### Metrics

- TeamCalculatedMetrics - per-side metrics model (M2A single-team fields plus
  M2B possession/opponent-dependent fields); see `Docs/03-domain-and-rules.md`
- CalculatedMetricsCalculator (Milestone 2A) - core numerical metrics derived
  from a single `TeamStats`
- GameCalculatedMetrics - Team + Opponent (`TeamCalculatedMetrics`) plus the
  M2C game-level `GameEstimatedPossessions`/`EstimatedPace`
- GameCalculatedMetricsCalculator (Milestone 2B + 2C) - possession estimates
  and opponent-dependent metrics for a Team/Opponent `TeamStats` pair,
  calculated symmetrically; an additional overload also taking
  `GameFormat`/`GameTiming` adds live estimated pace. Reuses
  CalculatedMetricsCalculator rather than duplicating it
- `StatRulesEngine`'s `LowEffectiveFieldGoalPercentage`, `LowFreeThrowRate`,
  `OffensiveEfficiencyProblem`, `TooManyThreePointAttempts`, and
  `LowDefensiveReboundPercentage` rules (Milestone 3) are the production
  consumers of this layer so far, reading `EffectiveFieldGoalPercentage`/
  `FreeThrowRate`/`OffensiveRating`/`ThreePointAttemptRate`/
  `ThreePointPercentage`/`DefensiveReboundPercentage` via
  `GameCalculatedMetricsCalculator` directly; the rest of M2A/M2B/M2C still
  has no consumer - see `Docs/03-domain-and-rules.md`
- `OpponentHotFromThree` (an M1 rule, trigger unchanged) also reads the
  opponent's `ThreePointPercentage` via `GameCalculatedMetricsCalculator`
  instead of `LegacyPercentageBridge` - a same-formula data-source migration,
  not a new or refined finding - see `Docs/03-domain-and-rules.md`
- `LowFreeThrowRate` replaced `LackOfPaintPressure`'s trigger (fouls + score,
  with no defensible connection to what it claimed to measure); the
  `LackOfPaintPressure` enum member is kept, but `StatRulesEngine` no longer
  triggers it - see `Docs/03-domain-and-rules.md`
- `OffensiveEfficiencyProblem`'s trigger was refined in place (same tag, no
  new enum member): it now reads `OffensiveRating` (points per 100 estimated
  possessions) with no score-margin gate, instead of raw FG% gated on losing
  by a margin - see `Docs/03-domain-and-rules.md`
- `TooManyThreePointAttempts`'s trigger was refined in place (same tag, no
  new enum member): its volume gate now reads `ThreePointAttemptRate`
  (3PA/FGA) instead of an absolute 3PA count, with a `FieldGoalsAttempted`
  minimum sample; the "shooting badly" gate (`TooManyThreePctMax`) is
  unchanged - see `Docs/03-domain-and-rules.md`
- `LowDefensiveReboundPercentage` replaced `DefensiveReboundProblem`'s
  trigger (an offensive-rebound differential that never read
  `team.DefensiveRebounds`, unlike the two refinements above); the
  `DefensiveReboundProblem` enum member is kept, but `StatRulesEngine` no
  longer triggers it - see `Docs/03-domain-and-rules.md`

---

## Design Philosophy

The Domain layer:

- contains no validation
- contains no configuration binding
- contains no infrastructure dependencies
- should change the least over time

Rules are deterministic, explainable, and testable.

---

## What This Project Does NOT Do

- No HTTP
- No persistence
- No AI integration
