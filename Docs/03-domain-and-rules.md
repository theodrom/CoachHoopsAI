# Domain and Rules

## Domain Concepts

- TeamStats (raw box-score counts: made/attempted shooting, rebounds, assists, turnovers, steals, blocks, fouls - not percentages)
- GameFormat (regulation periods/length, overtime length - the structure of a specific game)
- GameTiming (current period, clock remaining - where the game currently stands)
- ProblemTag
- Level
- Suggestion
- SuggestionCategory

### Level vs. GameFormat

`Level` (`EasyBasket`/`Youth`/`Amateur`/`Pro`) selects the analysis/rules
profile. `GameFormat` describes the actual structure of the game being played.
The two are independent: a Youth game may be played 4x10 or 4x5, and nothing in
the domain ties a `Level` to a specific format.

### Period numbering and overtime

`GameTiming.CurrentPeriod` is sequential: `1..GameFormat.RegulationPeriods` is
regulation; `RegulationPeriods + 1` is OT1, `+2` is OT2, and so on. Overtime
state (`IsOvertime`, `OvertimeNumber`) is always derived from `CurrentPeriod`
and `RegulationPeriods` - there is no separate overtime flag that could
contradict it.

## Rules Engine

StatRulesEngine evaluates:
- team stats
- opponent stats
- RulesProfile thresholds

Rules are:
- deterministic
- flat (no nesting)
- explainable

### Compatibility bridge

`StatRulesEngine`'s thresholds are written against shooting percentages, but
`TeamStats` stores only raw made/attempted counts. A `LegacyPercentageBridge`
computes field-goal and three-point percentage on the fly from those counts for
the engine to compare against. This exists to keep the current rules behaving
the same way after the raw-stat change - it is scaffolding, not a
general-purpose calculated-metrics layer, and is expected to be removed once
the rules engine is redesigned to work from raw counts directly.

## Calculated Metrics (Milestone 2)

Calculated metrics are a purely numerical layer - facts derived from raw
counts, with no thresholds, judgments, or presentation formatting. Ratios are
normalized decimals (`0.425`, not `42.5`), never rounded or clamped
internally. Formulas below are current defaults and are expected to evolve as
later milestones (M3+) add interpretation on top of them - this section
documents what M2 *calculates*, not what any of it *means*.

This is a separate concept from `LegacyPercentageBridge` above: the bridge is
temporary scaffolding for the existing rules engine's two percentages, while
`TeamCalculatedMetrics`/`GameCalculatedMetrics` are the new general-purpose
calculated-metrics layer. As of M2B this layer has no production consumer - it
is not yet wired into the rules engine, diagnostics, the LLM prompt, Admin,
persistence, or API responses.

### M2A - single-team metrics

`CalculatedMetricsCalculator.Calculate(TeamStats)` produces a
`TeamCalculatedMetrics` record from **one side's raw counts alone** - no
opponent, no `GameFormat`/`GameTiming`:

| Metric | Formula | Zero-denominator result |
|---|---|---|
| Field Goal % | `FGM / FGA` | `0.0` when `FGA == 0` |
| Three-Point % | `3PM / 3PA` | `0.0` when `3PA == 0` |
| Free-Throw % | `FTM / FTA` | `0.0` when `FTA == 0` |
| Total Rebounds | `OREB + DREB` | n/a |
| Effective FG % | `(FGM + 0.5 * 3PM) / FGA` | `0.0` when `FGA == 0` |
| Assist-to-Turnover Ratio | `AST / TO` | **`null`** when `TO == 0` |
| Three-Point Attempt Rate | `3PA / FGA` | `0.0` when `FGA == 0` |
| Free-Throw Rate | `FTA / FGA` | `0.0` when `FGA == 0` |

Assist-to-turnover is `null`, not `0`, because a zero-turnover performance has
no meaningful ratio - forcing it to zero would read as "turns the ball over
constantly," the opposite of what happened.

### M2B - possession- and opponent-dependent metrics

`GameCalculatedMetricsCalculator.Calculate(TeamStats team, TeamStats opponent)`
produces a `GameCalculatedMetrics` record (`Team` + `Opponent`, both
`TeamCalculatedMetrics`). It reuses `CalculatedMetricsCalculator` for each
side's M2A fields, then enriches both sides with the metrics below - it does
not re-derive the M2A formulas.

**Team and Opponent are calculated symmetrically.** Neither side is
analytically privileged: `Calculate(team, opponent).Team` is equivalent to
`Calculate(opponent, team).Opponent`, and vice versa. This matters because a
later milestone may apply different rule profiles per side.

Estimated possessions (per side, independently - this milestone does not
average the two sides into one game-possession value):

```text
EstimatedPossessions = FGA - OREB + TO + 0.44 * FTA
```

Remaining metrics, all `null` when the named denominator is zero (never forced
to `0` - a metric that divides by zero possessions or zero rebound
opportunities is not meaningfully observable, not literally zero):

| Metric | Formula | `null` when |
|---|---|---|
| Offensive Rating | `100 * Points / EstimatedPossessions` (own side) | own `EstimatedPossessions == 0` |
| Turnover Rate | `Turnovers / EstimatedPossessions` (own side) | own `EstimatedPossessions == 0` |
| Offensive Rebound % | `OREB / (OREB + Opponent DREB)` | `OREB + Opponent DREB == 0` |
| Defensive Rebound % | `DREB / (DREB + Opponent OREB)` | `DREB + Opponent OREB == 0` |
| Steal Rate | `Steals / Opponent EstimatedPossessions` | opponent's `EstimatedPossessions == 0` |
| Foul Rate | `PersonalFouls / Opponent EstimatedPossessions` | opponent's `EstimatedPossessions == 0` |

Turnover Rate intentionally uses `Turnovers / EstimatedPossessions`, not the
alternative `TO / (FGA + 0.44 * FTA + TO)` - the two are different current
defaults, and CoachHoopsAI's is the possession-based one above. Foul Rate is a
practical current default, not a claim of one canonical basketball
definition. `EstimatedPossessions` stays a `double` throughout - it is never
projected to an integer.

## Philosophy

Rules represent �coach-agreeable� heuristics, not absolute truth.
They are signals for AI prompting, not final judgments.
