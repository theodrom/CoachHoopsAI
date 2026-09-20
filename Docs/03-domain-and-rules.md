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
calculated-metrics layer. As of the fourth Milestone 3 slice, `StatRulesEngine`
reads `EffectiveFieldGoalPercentage`, `FreeThrowRate`, `OffensiveRating`,
`ThreePointAttemptRate`, and `ThreePointPercentage` from this layer, for
`LowEffectiveFieldGoalPercentage`, `LowFreeThrowRate`,
`OffensiveEfficiencyProblem`, and `TooManyThreePointAttempts` respectively
(see "Findings (Milestone 3)" below). Everything else in M2A/M2B/M2C still
has no production consumer - it is not wired into diagnostics, the LLM
prompt, Admin, persistence, or API responses. Interpreting the rest of these
numbers (is this pace good, is this rebound rate a problem) remains M3 scope
and is not implemented yet.

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
and the per-level values in `CoachHoopsAI.Api/appsettings.json`. The attempts
minimum scales down for levels with shorter game formats (fewer total
field-goal attempts per game), mirroring how `TooManyThreeAttemptsMin`
already scales per level.

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

### `OffensiveEfficiencyProblem` (refined, same tag)

**Old trigger:**

```text
(opponent.Points - team.Points) >= profile.LossByPointsToFlagOffensiveEfficiency
  AND
LegacyPercentageBridge.FieldGoalPercentage(team) <= profile.OurLowFieldGoalPctForOffensiveEfficiency
```

Required **both** losing by a score margin **and** a low raw FG%. Raw FG%
ignores three-pointers and free throws entirely, and the margin gate meant a
team could have a genuinely poor offense in a close game or a win and never
be flagged, while a team that lost badly for unrelated reasons (turnovers,
rebounding, fouls) with merely mediocre shooting could be.

**New trigger:**

```text
teamMetrics.EstimatedPossessions >= profile.OurLowOffensiveRatingPossessionsMin
  AND
teamMetrics.OffensiveRating.HasValue
  AND
teamMetrics.OffensiveRating.Value <= profile.OurLowOffensiveRating
```

where `teamMetrics` is `GameCalculatedMetricsCalculator.Calculate(team, opponent).Team`
(M2B) - `OffensiveRating` (points per 100 `EstimatedPossessions`) is only
populated by the two/four-argument game-level calculator, not
`CalculatedMetricsCalculator.Calculate(team)` alone, so `StatRulesEngine` now
computes `teamMetrics` via the M2B calculator instead (its `.Team` still
carries every M2A field unchanged, so `LowEffectiveFieldGoalPercentage` and
`LowFreeThrowRate` above are unaffected by this switch).

**The score-margin gate is removed entirely.** A team's offense can be
inefficient whether the team is losing, tied, or winning; requiring a loss
first was never a defensible precondition for that finding, so it no longer
gates anything here.

**Why the same tag, not a new one - unlike `LackOfPaintPressure`.** The
distinguishing test applied to both decisions: did the old trigger measure a
plausible, if crude, version of what the tag's name claims, or something
essentially unrelated? `LackOfPaintPressure`'s old trigger (fouls + score)
had no monotonic relationship to interior scoring at all - that was retired.
`OffensiveEfficiencyProblem`'s old trigger (low raw FG% narrowly gated on
losing) *is* a crude, under-inclusive proxy for the same concept
`OffensiveRating` now measures directly - both are fundamentally about "the
offense did not score efficiently." Reusing the tag here completes, rather
than contradicts, the concept it already named. `AnalysisHistoryService.RulesetVersion`
was bumped (`1.3` -> `1.4`) to mark that the trigger changed.

**Do not emit when the rating is unavailable or possessions are
non-positive.** `OffensiveRating` is `null` only when `EstimatedPossessions == 0`
exactly (M2B's zero-denominator convention). A *negative* `EstimatedPossessions`
(possible today - see the open `EstimatedPossessions` validation question
under "Next steps (M3)" in `CLAUDE.md`) is a **different** case: it produces
a non-null but meaningless `OffensiveRating`, which a null check alone would
not catch. `OurLowOffensiveRatingPossessionsMin` being a positive threshold
handles both in one comparison - zero, negative, and merely-too-small
possession counts all fail `EstimatedPossessions >= PossessionsMin`. This
rule does not change or add any input-validation policy; it only refuses to
draw a conclusion from a possession estimate it cannot trust.

**Minimum possessions gate**, not an attempts gate: `OffensiveRating`'s
denominator is `EstimatedPossessions`, not `FieldGoalsAttempted`, so the
sample-size gate is possession-based (`OurLowOffensiveRatingPossessionsMin`,
a `double` since `EstimatedPossessions` is never rounded to an integer -
current default 20.0 at Amateur level), for the same small-live-sample
reason as the attempts gates above, and mirrors their per-level values.

**Distinct from `LowEffectiveFieldGoalPercentage`.** Shooting efficiency
(eFG%) and points per estimated possession (`OffensiveRating`) can both be
low on the same team/game, or only one can be - a team can shoot well but
still have a low rating if turnovers inflate its possession count far beyond
its shot attempts, and a team can shoot poorly while still posting an
adequate rating if it draws enough free throws or limits turnovers. Neither
metric identifies shot selection, spacing, pace, or any other tactical cause;
both describe a measured result only.

Thresholds (`RulesProfile.OurLowOffensiveRating`/`OurLowOffensiveRatingPossessionsMin`,
and the per-level values in `CoachHoopsAI.Api/appsettings.json`) are current
defaults, not universal basketball facts - real offensive-rating baselines
vary enormously by level and style of play, and these values are a starting
point expected to evolve.

### `TooManyThreePointAttempts` (refined, same tag)

**Old trigger:**

```text
team.ThreePointsAttempted >= profile.TooManyThreeAttemptsMin
  AND
LegacyPercentageBridge.ThreePointPercentage(team) <= profile.TooManyThreePctMax
```

Gated volume on an **absolute** 3PA count (`TooManyThreeAttemptsMin`, 30 at
Amateur). The same raw count means something different depending on total
shot volume: 30 three-point attempts out of 60 total field-goal attempts is
half the offense; 30 out of 100 is well under a third. An absolute count
can't tell those apart.

**New trigger:**

```text
team.FieldGoalsAttempted >= profile.TooManyThreeAttemptRateAttemptsMin
  AND
teamMetrics.ThreePointAttemptRate >= profile.TooManyThreeAttemptRateMin
  AND
teamMetrics.ThreePointPercentage <= profile.TooManyThreePctMax
```

`ThreePointAttemptRate` (`3PA / FGA`) and `ThreePointPercentage` (`3PM / 3PA`)
are both M2A fields, already calculated by `CalculatedMetricsCalculator`
before this milestone - this rule is the first to read either of them, and
the second `TooManyThreePointAttempts`-family rule (after
`LowEffectiveFieldGoalPercentage`/`LowFreeThrowRate`/`OffensiveEfficiencyProblem`)
to move off `LegacyPercentageBridge`. `ThreePointPercentage`'s formula is
identical to `LegacyPercentageBridge.ThreePointPercentage` - switching the
source changes nothing about the "shooting badly" gate's behavior by itself.

**The volume gate is now a rate, not a count; the quality gate is
unchanged.** Reading `ThreePointAttemptRate` instead of a raw count makes the
volume side relative to the team's own shot diet, rather than an arbitrary
absolute number. `TooManyThreePctMax` (the accuracy requirement) was **not**
touched.

**Minimum field-goal-attempts gate**, mirroring the reasoning already used
for `LowEffectiveFieldGoalPercentage`/`LowFreeThrowRate` above:
`ThreePointAttemptRate`'s denominator is `FieldGoalsAttempted`, so a team
that has only taken a handful of shots so far - including early in a live,
in-progress game - could otherwise post an extreme rate (e.g. `2/2 = 1.0`)
from a trivial sample. `TooManyThreeAttemptRateAttemptsMin` (current default
20 at Amateur level) exists to prevent that.

**What the trigger does, and does not, establish.** A high
`ThreePointAttemptRate` combined with a `ThreePointPercentage` at or below
`TooManyThreePctMax` is **not proof that a different shot mix would have
scored more**. Three-point value is `3 * 3P%` points per attempt; at exactly
the Amateur threshold (0.33), that is `0.99` points per attempt - and a team
shooting, say, 30% from three (`0.90` points/attempt) can still be
outscoring what that same team actually does on its two-point attempts. This
rule has no data on the team's two-point value to compare against (nor does
any calculated metric here establish which alternative shots the team would
have taken instead), so it never makes that comparison, and no wording in
this codebase should imply that it does. The trigger is a **coaching
threshold worth a second look**, not a proven verdict that the shot
selection was wrong.

Because of this, every human- or model-facing surface for this tag is
deliberately phrased as an **observation**, not a judgment - "High
Three-Point Share With Low Three-Point Percentage," not "Too Many
Three-Point Attempts" - while the `ProblemTag.TooManyThreePointAttempts`
enum member keeps its original name regardless:

- **Admin** (`ProblemTagDto.MapTag`) shows "High Three Point Share With Low
  Three Point Percentage" as the display label for ordinal `4`.
- **The LLM prompt** (`OpenAiSuggestionClientHttp.LlmDescription`) sends
  "High three-point share with low three-point percentage" in place of the
  bare enum name for this one tag - a model given only the raw identifier
  `TooManyThreePointAttempts` has no way to know it's a judgment threshold
  rather than a settled fact, so the substitution happens before the prompt
  is built. Every other `ProblemTag`'s prompt value is still its bare enum
  name (which, for the rest of the enum, already reads as a neutral fact).
  This substitution is one-directional and prompt-only: the internal-identifier
  leak filter (`ExposesInternalIdentifier`) deliberately keeps checking the raw
  `TooManyThreePointAttempts` string, not the neutral phrase, for what it
  rejects. Those are two different concerns - what the prompt *sends* the
  model vs. what the filter *refuses to let through* - and conflating them
  would either miss a genuine identifier leak (if the model reproduces the raw
  name from general pattern-matching rather than by copying this prompt) or
  wrongly discard a valid suggestion for using the neutral phrase, which is
  the sanctioned coaching-language wording for this finding, not a leak.
- **The API response** (`AnalyzeGameMappings.ToResponseDto`) and **persisted
  history** (`AnalysisRecord.ProblemTagsJson`) are deliberately left
  unchanged: they still emit/store the literal `TooManyThreePointAttempts`
  name/ordinal. It is a legacy identifier consumers may already depend on,
  and renaming it would either break that stability or require yet another
  tag (see the ordinal-safety note above) - so its *documented* meaning is
  corrected here and in `Docs/02-api-contracts.md` instead of its *literal*
  spelling. An external API consumer reading only the raw string should
  consult that documentation before treating the name at face value.

**Why the same tag, not a new one.** Same test as the two decisions above:
the old trigger (an absolute count paired with the same accuracy gate) was a
narrower, less context-aware version of the same volume-plus-accuracy
signal the new trigger measures more precisely - not something conceptually
unrelated. `AnalysisHistoryService.RulesetVersion` was bumped (`1.4` ->
`1.5`) to mark that the trigger changed.

**Distinct from `OurShootingInefficiency`.** `OurShootingInefficiency` is
about three-point *accuracy* alone (`3P% <= OurBadThreePct`, gated on a 3PA
volume minimum) - it can fire on a team that barely shoots threes at all, as
long as the handful they take mostly miss. `TooManyThreePointAttempts` is
about three-point *reliance* - it only fires when threes make up a large
share of the team's total shot diet (`ThreePointAttemptRate`), combined with
a below-threshold accuracy. A team can be inefficient from three without
shooting them often enough to trip the volume gate here, and a team can trip
this rule without being as bad as `OurBadThreePct`'s stricter accuracy bar
requires (the two percentage thresholds differ: `TooManyThreePctMax` 0.33
vs. `OurBadThreePct` 0.30 at Amateur level) - the two findings can fire
together, independently, or not at all. Neither finding compares against the
team's two-point value either, for the same reason described above.

Thresholds (`RulesProfile.TooManyThreeAttemptRateMin`/
`TooManyThreeAttemptRateAttemptsMin`/`TooManyThreePctMax`, and the per-level
values in `CoachHoopsAI.Api/appsettings.json`) are current defaults, not
universal basketball facts - and, per the above, a coaching-judgment cutoff
rather than a mathematically-derived break-even point.

### `DefensiveReboundProblem` (retired) and `LowDefensiveReboundPercentage` (its replacement)

**Old trigger:**

```text
opponent.OffensiveRebounds - team.OffensiveRebounds >= profile.OpponentOffensiveReboundDiffToFlag
```

Auditing this against the data it reads: **both terms are OFFENSIVE rebound
counts** - the opponent's second-chance recoveries on their own misses, and
our own second-chance recoveries on our own misses. `team.DefensiveRebounds`,
the one raw stat that actually describes our defensive rebounding, was
never read at all. A team with a genuine defensive-rebounding problem could
dodge this flag simply by also offensive-rebounding well (which shrinks the
differential but says nothing about defense), and a team with perfectly
adequate defensive rebounding could trip it by having a quiet night on its
own offensive glass. The name promised a defensive-rebounding finding the
trigger never measured.

**Decision: retired, not reused.** Same test as `LackOfPaintPressure`'s
retirement above, and the opposite conclusion from `OffensiveEfficiencyProblem`'s
and `TooManyThreePointAttempts`' refinements: did the old trigger read data
that is at least thematically related to what the tag's name claims, or
something essentially unrelated? Both of those reused triggers read data
squarely on-topic (shooting percentage for an efficiency finding, 3PA volume
for a three-point-volume finding), just measured imprecisely.
`DefensiveReboundProblem`'s old trigger reads zero defensive-rebounding data;
it is an offensive-rebounding differential, a different phase of the game
entirely, mislabeled. Reusing the tag with a corrected trigger would have
made `ProblemTag.DefensiveReboundProblem` mean two unrelated things across a
single database's history. Instead:

- `StatRulesEngine` no longer triggers `DefensiveReboundProblem` at all.
- The enum member is kept, never removed, so existing analysis records that
  contain it (persisted as the raw ordinal `6`) keep resolving to a real
  label instead of `Unknown(6)` in Admin.
- `AnalysisHistoryService.RulesetVersion` was bumped (`1.5` -> `1.6`) to mark
  that the active rule set changed under these records.
- `RulesProfile.OpponentOffensiveReboundDiffToFlag`, the old trigger's only
  threshold field, was removed rather than left unused (it had no other
  consumer).

**Replacement: `LowDefensiveReboundPercentage`.** M2B already calculates
exactly the right metric for this tag's intended meaning:
`DefensiveReboundPercentage = TeamDREB / (TeamDREB + OpponentOREB)` - the
share of **available defensive-rebound opportunities** (our own defensive
rebounds plus the opponent's offensive rebounds - the boards that were
missed and up for grabs on our defensive end) that we actually secured. The
new trigger:

```text
team.DefensiveRebounds + opponent.OffensiveRebounds >= profile.OurLowDefensiveReboundPctOpportunitiesMin
  AND
teamMetrics.DefensiveReboundPercentage.HasValue
  AND
teamMetrics.DefensiveReboundPercentage.Value <= profile.OurLowDefensiveReboundPct
```

where `teamMetrics` is the same `GameCalculatedMetricsCalculator.Calculate(team, opponent).Team`
already computed for the other M3 rules above - no new calculator call.

**Minimum-opportunities gate**, mirroring `OffensiveEfficiencyProblem`'s
`PossessionsMin` reasoning: without it, a team a handful of rebounds into a
live, in-progress game could post an extreme percentage (e.g. `1/1 = 1.0` or
`0/0` undefined) from a trivial sample. Unlike `EstimatedPossessions` (a
subtraction that can go negative even on valid non-negative raw input, see
the open validation question in `CLAUDE.md`), this denominator is a **sum**
of two non-negative counts, so `DefensiveReboundPercentage` is only `null`
when both `TeamDREB` and `OpponentOREB` are exactly zero - there is no
"negative opportunities" edge case analogous to `OffensiveEfficiencyProblem`'s.
`OurLowDefensiveReboundPctOpportunitiesMin` still matters, though: it's the
same small-live-sample protection as every other M3 minimum-sample gate, not
just a null check.

**Coach-facing framing.** The finding describes how many of the available
defensive rebounds the team actually secured - a measured share, not an
assignment of blame. It does **not** claim that a particular positioning
error, boxing-out technique, or lack of effort caused the result; `TeamStats`
has no positioning, assignment, or possession-by-possession data to support
attributing a cause. Unlike `TooManyThreePointAttempts`, the new tag's bare
name (`LowDefensiveReboundPercentage`) does not overclaim anything on its
own - "low percentage" is already a literal description, not an implicit
verdict - so no Admin-label or LLM-prompt substitution was needed for it;
`ProblemTagDto.MapTag` uses the same literal Title Case as
`LowEffectiveFieldGoalPercentage`/`LowFreeThrowRate`, and the LLM prompt
sends its bare enum name like every tag that doesn't need special handling.
The retired `DefensiveReboundProblem` needs no such substitution either,
since `StatRulesEngine` never emits it for new analyses - it only appears in
already-persisted history, which is never re-sent to the LLM.

Thresholds (`RulesProfile.OurLowDefensiveReboundPct`/
`OurLowDefensiveReboundPctOpportunitiesMin`, and the per-level values in
`CoachHoopsAI.Api/appsettings.json`) are current defaults, not universal
basketball facts.

`LowDefensiveReboundPercentage` was appended at the very end of the
`ProblemTag` enum (after `LowFreeThrowRate`), for the same ordinal-safety
reason documented above.

### `OpponentHotFromThree` (unchanged meaning, migrated data source)

Unlike every other rule in this section, `OpponentHotFromThree`'s trigger was
already sound before Milestone 3 touched it:

```text
opponentThreePointPct >= profile.OpponentHotThreePct
  AND
opponent.ThreePointsAttempted >= profile.OpponentHotThreeAttemptsMin
```

It reads the correct side's data (the opponent's three-point shooting, not
ours), it already carries a minimum-attempts gate so a handful of early
attempts can't read as "hot" from a trivial sample, and the trigger itself
never depended on score margin, fouls, or any other data unrelated to
three-point shooting. The name is shorthand for a measured result - "the
opponent made a high share of a meaningful number of three-point attempts" -
not a tactical diagnosis; nothing here infers poor closeouts, blown
rotations, weak-side rotation lapses, or shot quality, none of which
`TeamStats` has the shot-location or possession-by-possession data to
support. `ProblemTagDto`'s label ("Opponent Hot From Three") and the LLM
prompt (which sends this tag's bare enum name, like every tag other than
`TooManyThreePointAttempts`) both already reflect that.

**The only change: `opponentThreePointPct` moved off `LegacyPercentageBridge`
onto `gameMetrics.Opponent.ThreePointPercentage`** (M2A's formula, exposed
per-side by `GameCalculatedMetricsCalculator`/M2B, which `StatRulesEngine`
already calls once per `Evaluate` for the rules above). The two formulas are
identical -
`ThreePointsAttempted == 0 ? 0.0 : ThreePointsMade / ThreePointsAttempted` -
including the same zero-attempts convention (`0.0`, never `null`, unlike the
M2B-only fields such as `DefensiveReboundPercentage`), so this is a
mechanically equivalent migration: same trigger, same per-level thresholds,
same tag, no `RulesetVersion` bump. At the time this migration landed,
`PerimeterDefenseProblem` also read `opponentThreePointPct` (against its own
hardcoded 0.36 threshold) and was left unchanged - it has since been retired
outright; see the next section.

No `RulesProfile` fields were added, removed, or renamed; no enum member was
added; no Admin label or LLM-prompt wording changed, because none of them
overclaimed anything to begin with.

### `PerimeterDefenseProblem` (retired, no replacement)

**Old trigger:**

```text
opponentThreePointPct >= 0.36
  AND
opponent.ThreePointsAttempted >= team.ThreePointsAttempted + 5
```

where `opponentThreePointPct` was `LegacyPercentageBridge.ThreePointPercentage(opponent)`
(this rule was never migrated onto the M2 calculator, unlike `OpponentHotFromThree`
above).

Auditing this against `OpponentHotFromThree`'s trigger reveals it reads the
exact same two facts - the opponent's three-point percentage and volume -
just measured more loosely:

- **Threshold**: a hardcoded `0.36`, bypassing `RulesProfile` entirely. Every
  other threshold in `StatRulesEngine`, including `OpponentHotFromThree`'s own
  `OpponentHotThreePct` (0.38-0.40 depending on level), is profile-driven so
  it can be tuned per competition level; this one could not be.
- **Sample-size gate**: `opponent.ThreePointsAttempted >= team.ThreePointsAttempted + 5`
  is relative to our own volume, not an absolute floor. If our team had not
  yet attempted a three, as few as **5** opponent attempts satisfied it - not
  a meaningful sample for a percentage judgment, and far weaker than
  `OpponentHotFromThree`'s real, per-level `OpponentHotThreeAttemptsMin`
  (12-25 depending on level).
- **Name**: unlike `OpponentHotFromThree`'s literal framing, "perimeter
  defense problem" is a tactical diagnosis - it implies a cause (closeouts,
  rotations, communication, contest quality, defensive positioning).
  `TeamStats` has no shot-location, assignment, or possession-by-possession
  data to establish any of those; opponent 3P% only measures a shooting
  outcome, not why it happened. Applying the M3 rule established throughout
  this document - a finding may state a measured result, never assert an
  unsupported cause - this trigger fails on the same grounds as the retired
  `LackOfPaintPressure`/`DefensiveReboundProblem` triggers.

**Decision: retired outright, no replacement tag.** Unlike
`LowFreeThrowRate`/`LowDefensiveReboundPercentage`, which replaced their
retired predecessors with a corrected trigger on genuinely different or more
precise data, `PerimeterDefenseProblem` reads *identical* evidence to
`OpponentHotFromThree` - a rule that already exists, is already correctly
literal, and is already profile-tunable. Reusing the ordinal with a corrected
trigger, or appending a new tag, would have created a duplicate finding for
the same underlying signal under a second name. Instead:

- `StatRulesEngine` no longer triggers `PerimeterDefenseProblem` at all; the
  `if` block was removed, along with the `opponentThreePointPct` local
  variable it was the sole remaining reader of (`OpponentHotFromThree` reads
  `gameMetrics.Opponent.ThreePointPercentage` instead, per the migration
  above).
- The enum member is kept, never removed, so existing analysis records that
  contain it (persisted as the raw ordinal `8`) keep resolving to a real
  label instead of `Unknown(8)` in Admin.
- `AnalysisHistoryService.RulesetVersion` was bumped (`1.6` -> `1.7`) to mark
  that the active rule set changed under these records.
- No `RulesProfile` field is removed, because none ever backed this trigger -
  its threshold and gate were hardcoded literals, not configuration.
- `OpponentHotFromThree` itself is unchanged by this retirement. The audit
  found no coverage gap worth compensating for: the narrow band where the old
  rule fired but `OpponentHotFromThree` would not (opponent 3P% between 0.36
  and the applicable `OpponentHotThreePct`) was only reachable through the old
  rule's undersized 5-attempt gate - a defect, not a signal worth preserving.
  Broadening `OpponentHotFromThree` to reclaim that band would reintroduce the
  same thin-sample problem this retirement removes.
- `PerimeterDefenseProblem` was never given special Admin-label or LLM-prompt
  wording (unlike `TooManyThreePointAttempts`), so there is nothing to revert
  there; `StatRulesEngine` simply stops emitting it for new analyses; only
  already-persisted history can contain it, and that history is never
  re-sent to the LLM.

## Philosophy

Rules represent �coach-agreeable� heuristics, not absolute truth.
They are signals for AI prompting, not final judgments.
