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
calculated-metrics layer. As of the first Milestone 3 slice, `StatRulesEngine`
reads exactly one field from this layer - `EffectiveFieldGoalPercentage`, for
`LowEffectiveFieldGoalPercentage` (see "Findings (Milestone 3)" below).
Everything else in M2A/M2B/M2C still has no production consumer - it is not
wired into diagnostics, the LLM prompt, Admin, persistence, or API responses.
Interpreting the rest of these numbers (is this pace good, is this rebound
rate a problem) remains M3 scope and is not implemented yet.

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

### M2C - live estimated pace

Pace is a **descriptive tempo measurement, not a judgment** - it says nothing
about whether the observed tempo is good, bad, desirable, or under control.
Whether a given pace matters is level/profile/strategy-dependent interpretation
and belongs to a later milestone (M3+), not to M2.

`GameCalculatedMetricsCalculator.Calculate(TeamStats team, TeamStats opponent,
GameFormat format, GameTiming timing)` extends M2B with two game-level (not
per-side) values on `GameCalculatedMetrics`:

```text
GameEstimatedPossessions = (Team.EstimatedPossessions + Opponent.EstimatedPossessions) / 2

EstimatedPace = GameEstimatedPossessions * GameFormat.RegulationDuration.TotalMinutes
                / GameTiming.ElapsedGameTime(format).TotalMinutes
```

`GameEstimatedPossessions` averages the two sides' independent M2B possession
estimates into a single game-level tempo input, to reduce the approximation
discrepancy between the two estimates rather than treating either side as
authoritative. It does not replace or remove `Team.EstimatedPossessions` /
`Opponent.EstimatedPossessions`, which remain the per-side M2B values.

`ElapsedGameTime` is the existing Milestone 1 `GameTiming.ElapsedGameTime(format)`; the pace calculation does not reimplement any clock arithmetic.

**Overtime**: `ElapsedGameTime` already includes elapsed overtime time once a
game reaches OT. Estimated Pace still normalizes against `RegulationDuration`
(e.g. 40 minutes for a 4x10 format) - it does not extend the normalization
length by scheduled or elapsed overtime. During overtime, the metric therefore
answers "at this observed tempo, how many possessions would a regulation-length
game have," not "how many possessions has this game had so far." There is no
separate "overtime pace."

**Zero elapsed time**: `EstimatedPace` is `null` (never `0`, `NaN`, or
infinity) when `ElapsedGameTime(format) == 0` - at tip-off there is no elapsed
time from which a tempo can be estimated, and a pace of `0` would misread as
an observed dead-stop tempo rather than "no sample yet." This does not affect
`GameEstimatedPossessions`, which depends only on the supplied `TeamStats` and
is always computable.

`GameEstimatedPossessions` and `EstimatedPace` are symmetric under swapping
Team and Opponent - both are game-level values, not per-side ones, so
swapping the two `TeamStats` arguments leaves them unchanged.

The two-argument `Calculate(team, opponent)` overload from M2B is unchanged
and keeps working exactly as before; it simply cannot produce `EstimatedPace`
(always `null` on that path) since it is never given a `GameFormat`/
`GameTiming` - no dummy timing is substituted to force a value.

M2C completes the currently planned Milestone 2 numerical layer (M2A + M2B +
M2C). No further calculated-metrics slices are currently planned; the next
Milestone 2 area (`Docs/README.md` roadmap) is interpretation (M3).

## Findings (Milestone 3)

M3 findings are judgments built on top of M2's facts: a threshold applied to
a calculated metric, producing a `ProblemTag`. Unlike M2, findings are
allowed to say "this is worth a coach's attention" - but a finding still only
describes a **measured result**, never an asserted **cause**. "Our shooting
is inefficient" is a finding; "because of poor shot selection" is not - the
current box-score data can't establish shot selection, spacing, or defensive
pressure as a cause, so no rule claims one.

### `LowEffectiveFieldGoalPercentage`

The first rule to read the Milestone 2 calculated-metrics layer directly,
instead of `LegacyPercentageBridge`:

```text
team.FieldGoalsAttempted >= profile.OurLowEffectiveFieldGoalPctAttemptsMin
  AND
CalculatedMetricsCalculator.Calculate(team).EffectiveFieldGoalPercentage
  <= profile.OurLowEffectiveFieldGoalPct
```

**Why a new tag, not an existing one.** Two existing tags look related but
measure something different, and reusing either would have silently created
an overlapping/ambiguous finding:

- `OurShootingInefficiency` is specifically about **three-point** shooting
  (`3P% <= OurBadThreePct` with a `3PA` volume gate) - a different metric
  (3P%, not eFG%) and a narrower scope (three-point shots only).
- `OffensiveEfficiencyProblem` is gated on **losing by a margin**
  (`LossByPointsToFlagOffensiveEfficiency`) as well as raw FG% - it conflates
  "the team is losing badly" with "the team is shooting poorly," and it reads
  raw FG% rather than eFG%, so it doesn't credit made three-pointers the way
  eFG% does. A team that is losing for reasons unrelated to shooting (fouls,
  turnovers, rebounding) can trip it without an eFG% problem, and a team with
  poor eFG% but a close game never trips it at all.

`LowEffectiveFieldGoalPercentage` is unconditional (no score-margin gate) and
reads eFG% specifically, so it is a distinct, non-overlapping signal from
both.

**Minimum field-goal-attempts gate.** `CalculatedMetricsCalculator` returns
`EffectiveFieldGoalPercentage == 0.0` when `FieldGoalsAttempted == 0` (the
M2A zero-denominator convention - see M2A above). Without a volume gate, a
team that simply hadn't taken many shots yet would read as "maximally
inefficient" purely from a tiny or zero sample, which is a sample-size
artifact, not a real finding. `OurLowEffectiveFieldGoalPctAttemptsMin`
(current default 20 field-goal attempts, i.e. roughly a full game's worth at
Amateur level) exists to prevent that.

**Thresholds are current defaults, not universal basketball facts** - see
`RulesProfile.OurLowEffectiveFieldGoalPct`/`OurLowEffectiveFieldGoalPctAttemptsMin`
and the per-level values in `CoachHoopsAI.Api/appsettings.json`. The
percentage threshold is set a little above the corresponding
`OurLowFieldGoalPctForOffensiveEfficiency` (raw FG%) threshold per level,
since eFG% credits made three-pointers and so reads a few points higher than
raw FG% for any team that makes some; the attempts minimum scales down for
levels with shorter game formats (fewer total field-goal attempts per game),
mirroring how `TooManyThreeAttemptsMin` already scales per level.

**Enum ordinal note.** `ProblemTag` is persisted as a raw integer array
(`AnalysisRecord.ProblemTagsJson`, via `System.Text.Json`'s default enum
encoding, read back by `ProblemTagDto.MapTag(int)` in Admin) - not as
strings, unlike the API response's `ProblemTags` (which uses `.ToString()`).
`LowEffectiveFieldGoalPercentage` was therefore appended at the end of the
`ProblemTag` enum rather than grouped with the other Offense tags, so it
doesn't shift the ordinals - and therefore the meaning - of tags already
present in persisted analysis records. Any future `ProblemTag` addition must
do the same.

### `LackOfPaintPressure` (retired) and `LowFreeThrowRate` (its replacement)

**`PointsInPaint` does not exist in this codebase.** `TeamStats` has no
shot-location data at all - only made/attempted counts for field goals,
three-pointers, and free throws (see Milestone 1 above). There is no way to
isolate "points scored in the paint" from a team's total points or even from
its two-point makes (`FieldGoalsMade - ThreePointsMade` includes mid-range
looks, not just paint shots). This is true for both a final box score and a
live, in-progress one - it isn't a live-sample reliability gap, it's a
missing input the raw-stat model was never given. Inferring "paint points"
from total points alone (e.g. treating a low-scoring game as a paint-scoring
problem) would assert something the data cannot support, which is why no
rule here does that.

`LackOfPaintPressure`'s original trigger was:

```text
team.PersonalFouls <= opponent.PersonalFouls - 5
  AND
team.Points <= opponent.Points
```

Auditing this against the data it reads: **team fouls committed by the team
itself, and overall score**, have no defensible relationship to that team's
own interior/rim scoring. Fewer personal fouls than the opponent mostly
reflects defensive/foul discipline (a Defense-side concern, already covered
in spirit by `FoulsProblem`'s comparison in the other direction), not
offensive paint pressure; trailing on the scoreboard can happen for any
number of unrelated reasons (turnovers, rebounding, three-point defense).
The name promised an interior-scoring finding the trigger never measured.

**Decision: retired, not reused.** Reusing the tag with a new trigger would
have made `ProblemTag.LackOfPaintPressure` mean two different things across
the history of a single database - the old fouls/score heuristic for
existing records, something else for new ones - which conflicts with this
project's stance against silently overlapping/reinterpreted findings (see
`LowEffectiveFieldGoalPercentage` above). Instead:

- `StatRulesEngine` no longer triggers `LackOfPaintPressure` at all.
- The enum member is kept, never removed, so existing analysis records that
  contain it (persisted as the raw ordinal `5`) keep resolving to a real
  label instead of `Unknown(5)` in Admin.
- `AnalysisHistoryService.RulesetVersion` was bumped (`1.2` -> `1.3`) to mark
  that the active rule set changed under these records.

**Replacement: `LowFreeThrowRate`.** This is a literal finding, not an
interior-scoring one: the team attempted few free throws relative to its
field-goal attempts. It does not claim, prove, or imply low interior/rim
aggression, low paint pressure, or any other cause. Free throws arise from
several distinct situations besides drives to the rim (post-ups away from
the basket, and-one calls on catch-and-shoot attempts, deliberate late-game
fouling by the opponent, shooting fouls beyond the paint), and plenty of
real paint attacks draw no whistle at all - so free-throw rate cannot stand
in for "how often did we attack the paint," only for "how often did we reach
the line." The name deliberately avoids the word "paint" (or "aggression",
"rim pressure", etc.) for exactly this reason - it names the metric it
reads (`FTA / FGA`, already calculated by M2A as
`TeamCalculatedMetrics.FreeThrowRate`), not an interpretation of it:

```text
team.FieldGoalsAttempted >= profile.OurLowFreeThrowRateAttemptsMin
  AND
CalculatedMetricsCalculator.Calculate(team).FreeThrowRate
  <= profile.OurLowFreeThrowRate
```

Same minimum-attempts reasoning as `LowEffectiveFieldGoalPercentage`:
`FreeThrowRate == 0.0` when `FieldGoalsAttempted == 0` (M2A's
zero-denominator convention), so without `OurLowFreeThrowRateAttemptsMin`
(current default 20 at Amateur level) a team that simply hasn't shot much
yet - including early in a live, in-progress game - would misread as
"maximally low" from a trivial sample. This is exactly the small-live-sample
case the gate exists to prevent.

Thresholds (`RulesProfile.OurLowFreeThrowRate`/`OurLowFreeThrowRateAttemptsMin`,
and the per-level values in `CoachHoopsAI.Api/appsettings.json`) are current
defaults, not universal basketball facts - real free-throw-rate baselines
vary by level, whistle tendencies, and play style, and these values are a
starting point expected to evolve. `OurLowFreeThrowRateAttemptsMin` mirrors
`OurLowEffectiveFieldGoalPctAttemptsMin`'s per-level values, since both gate
the same denominator (`FieldGoalsAttempted`) for the same reason.

`LowFreeThrowRate` was appended at the very end of the `ProblemTag` enum
(after `LowEffectiveFieldGoalPercentage`), for the same ordinal-safety reason
documented above.

## Philosophy

Rules represent �coach-agreeable� heuristics, not absolute truth.
They are signals for AI prompting, not final judgments.
