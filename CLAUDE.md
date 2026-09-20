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
- Verified 2026-09-20: `dotnet build CoachHoopsAI.sln` succeeds with no
  errors; `dotnet test CoachHoopsAI.sln` passes 256 tests, 0 failed
  (159 + 14 + 15 + 68 across the four test projects below). Treat this as a
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
- Six M3 rules (`LowEffectiveFieldGoalPercentage`, `LowFreeThrowRate`,
  `OffensiveEfficiencyProblem`, `TooManyThreePointAttempts`,
  `LowDefensiveReboundPercentage`, `HighOpponentEffectiveFieldGoalPercentage`;
  see below) now read M2A/M2B's `EffectiveFieldGoalPercentage`/
  `FreeThrowRate`/`OffensiveRating`/`ThreePointAttemptRate`/
  `ThreePointPercentage`/`DefensiveReboundPercentage` directly. Everything
  else in M2A/B/C is still not wired into diagnostics, the LLM prompt, Admin,
  persistence, or API responses - the rest of that integration is later M3
  work.

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
  place of it - this is still true, even though every rule that used to call
  it has now either migrated or been retired. Six M3 rules
  (`LowEffectiveFieldGoalPercentage`, `LowFreeThrowRate`,
  `OffensiveEfficiencyProblem`, `TooManyThreePointAttempts`,
  `LowDefensiveReboundPercentage`, `HighOpponentEffectiveFieldGoalPercentage`)
  read `GameCalculatedMetricsCalculator` directly by design, as new/refined
  M3 slices. `OpponentHotFromThree` and `OurShootingInefficiency` (both M1
  rules, triggers unchanged) each had just their own percentage side migrated
  the same way - same trigger, same thresholds, same meaning, so these are
  data-source migrations, not additional M3 slices, and not a signal to
  migrate the rest of `StatRulesEngine` yet. `PerimeterDefenseProblem` and
  `InteriorDefenseProblem`, the bridge's other former M1 consumers (of
  `ThreePointPercentage` and `FieldGoalPercentage` respectively), are both
  retired (see below) rather than migrated - `StatRulesEngine` no longer
  evaluates either at all, on the bridge or otherwise. As a result,
  `LegacyPercentageBridge.ThreePointPercentage` now has **no live caller
  anywhere in `StatRulesEngine`**, and `LegacyPercentageBridge.FieldGoalPercentage`
  is called once but only assigned to an unused local (`teamFieldGoalPct`) -
  a pre-existing dead assignment, left as-is and out of scope for these
  reviews. `LegacyPercentageBridge` itself is not deleted, since it remains
  intentional M1 scaffolding per the rule above until a milestone decision
  says otherwise, not something to remove opportunistically mid-review.
- `GameFormat`/`GameTiming` are captured and persisted but are **intentionally
  not yet consumed** by the rules engine, diagnostics, or the LLM prompt.

## Milestone roadmap

- **M2** - calculated numerical basketball metrics (facts derived from raw
  stats). Complete: M2A, M2B, M2C.
- **M3 (in progress)** - interpretation/findings/rules grounded in M2's
  calculated metrics. Judgments and thresholds belong here, not M2. Slices
  landed: `LowEffectiveFieldGoalPercentage`, `LowFreeThrowRate` (replaces
  `LackOfPaintPressure`'s trigger), `OffensiveEfficiencyProblem` (refined
  in place - same tag, `OffensiveRating`-based, no score-margin gate),
  `TooManyThreePointAttempts` (refined in place - same tag, rate-based
  volume gate instead of an absolute count), `LowDefensiveReboundPercentage`
  (replaces `DefensiveReboundProblem`'s trigger), `PerimeterDefenseProblem`
  retired with no replacement tag (same evidence as `OpponentHotFromThree`,
  just a weaker, non-profile-driven trigger),
  `HighOpponentEffectiveFieldGoalPercentage` (replaces `InteriorDefenseProblem`'s
  trigger - unlike `PerimeterDefenseProblem`, this signal was not already
  covered elsewhere, so it was replaced rather than retired outright),
  `TransitionDefenseProblem` retired with no replacement tag (its only
  measurable content - elevated team turnovers - was already covered by
  `TurnoverProblem`), `FoulsProblem` refined in place (same tag - the
  differential trigger was sound but under-inclusive, so an absolute
  foul-count condition was added alongside it; deliberately not migrated to
  `FoulRate`), `OurShootingInefficiency` found sound as-is (same tag, no
  trigger change) with only its calculation source migrated and its
  overclaiming "shooting" wording corrected to "Low Three Point Percentage."
- **M4** - sessions/snapshots.
- **M5** - LLM/Admin integration built on the above.

## Next steps (M3)

Ten slices complete, all in `Docs/03-domain-and-rules.md`'s "Findings
(Milestone 3)" section for full rationale:

- `LowEffectiveFieldGoalPercentage` (`StatRulesEngine`) flags a low team
  effective field-goal % (M2A), gated on a minimum field-goal-attempts sample
  size so a small/zero sample can't read as "inefficient."
- `LowFreeThrowRate` replaces `LackOfPaintPressure`'s trigger. `PointsInPaint`
  does not exist anywhere in this codebase - `TeamStats` has no shot-location
  data, for either a final or a live analysis - so no rule infers paint
  scoring from points alone. `LackOfPaintPressure`'s old trigger (personal
  fouls far below the opponent's, while not leading on score) had no
  defensible connection to interior/rim pressure; `StatRulesEngine` no longer
  triggers it, but the enum member and its Admin display mapping are kept for
  already-persisted records. `LowFreeThrowRate` (M2A `FreeThrowRate`,
  FTA/FGA, with the same kind of minimum-attempts gate) is a purely literal
  finding - few free-throw attempts relative to field-goal attempts - and
  makes no claim about interior aggression or paint pressure: free throws
  arise from several situations besides paint drives, and real paint attacks
  often draw no whistle at all, so the rate cannot stand in for paint
  activity. It was appended as a new tag rather than reusing the old ordinal,
  so the same `ProblemTag` value never means two different things across a
  database's history.
- `OffensiveEfficiencyProblem`'s trigger was refined in place - same tag, no
  new enum member. It previously required BOTH losing by a score margin AND a
  low raw FG%; it now reads `OffensiveRating` (points per 100
  `EstimatedPossessions`, M2B) directly, with no score-margin gate. Unlike
  `LackOfPaintPressure`, the tag was reused rather than retired: the old
  trigger was a narrow, score-gated proxy for the same underlying concept
  `OffensiveRating` now measures directly, not something conceptually
  unrelated. It does not emit when `OffensiveRating` is unavailable (`null`,
  only when `EstimatedPossessions == 0` exactly) or when `EstimatedPossessions`
  is non-positive (a negative estimate - see the open validation question
  below - would otherwise produce a non-null but meaningless rating); one
  possessions-based minimum-sample gate covers both. `RulesProfile`'s two
  dead fields from the old trigger
  (`LossByPointsToFlagOffensiveEfficiency`/`OurLowFieldGoalPctForOffensiveEfficiency`)
  were removed rather than left unused.
- `TooManyThreePointAttempts`'s volume gate was refined in place - same tag,
  no new enum member. It previously required an **absolute** 3PA count
  (`ThreePointsAttempted >= TooManyThreeAttemptsMin`); the same raw count
  meant something different depending on total shot volume (30 of 60 shots
  is half the offense, 30 of 100 is well under a third). It now reads
  `ThreePointAttemptRate` (`3PA/FGA`, M2A) directly, gated on a
  `FieldGoalsAttempted` minimum sample; the accuracy gate (`TooManyThreePctMax`)
  is unchanged. **Important correction:** a high rate plus a below-threshold
  `ThreePointPercentage` does not prove the shot mix scores worse than the
  alternative - e.g. 30% from three is 0.9 points/attempt, which can still
  beat that team's actual two-point efficiency, and this rule has no
  two-point value to compare against. It is a coaching-judgment threshold,
  not a verdict. The coach-facing label was changed accordingly to
  "High Three Point Share With Low Three Point Percentage" (an observation).
  The same substitution was later extended to the **LLM prompt**
  (`OpenAiSuggestionClientHttp.LlmDescription`): the bare enum name
  `TooManyThreePointAttempts` reads as a resolved verdict to a model that
  only has the raw identifier to go on, so the prompt sends the neutral
  phrase for this one tag instead - every other `ProblemTag`'s prompt value
  is still its bare enum name. **The internal-identifier leak filter
  (`ExposesInternalIdentifier`) was deliberately NOT changed to match** - it
  still rejects the raw `TooManyThreePointAttempts` string if the model
  produces it, and does not treat the neutral phrase as a leak, since that
  phrase is the sanctioned coaching-language wording for this finding, not
  an internal identifier; checking the transmitted description instead of
  the raw name was tried and reverted, because it would have both missed a
  genuine identifier leak and wrongly discarded valid suggestions for using
  the approved wording. `ProblemTag.TooManyThreePointAttempts` keeps its
  original name as the stable code/ordinal for the API response
  (`AnalyzeGameMappings`) and persisted history
  (`AnalysisRecord.ProblemTagsJson`) - **only its documented meaning was
  corrected, not its literal spelling in those two places**; see
  `Docs/02-api-contracts.md` for the API-consumer-facing note. Distinct from
  `OurShootingInefficiency` (accuracy alone, no volume-share requirement) -
  see `Docs/03-domain-and-rules.md` for the full comparison.
- `LowDefensiveReboundPercentage` replaces `DefensiveReboundProblem`'s
  trigger - same test as `LackOfPaintPressure`'s retirement, opposite
  conclusion from `OffensiveEfficiencyProblem`'s/`TooManyThreePointAttempts`'s
  refinements. The old trigger
  (`opponent.OffensiveRebounds - team.OffensiveRebounds >= OpponentOffensiveReboundDiffToFlag`,
  field removed) compared two OFFENSIVE rebound counts and never read
  `team.DefensiveRebounds` at all - a team with a genuine defensive-rebounding
  problem could dodge the flag simply by also offensive-rebounding well.
  Unlike the two reused refinements (which read data at least thematically
  on-topic), this had no defensible connection to what the tag claims, so it
  was retired rather than reused - same pattern as `LackOfPaintPressure`. The
  new tag reads `DefensiveReboundPercentage` (`TeamDREB / (TeamDREB +
  OpponentOREB)`, M2B) directly: our share of available defensive-rebound
  opportunities. A minimum-opportunities gate applies, though this
  denominator (a sum of two non-negative counts) can only be non-positive
  when both terms are exactly zero, unlike `EstimatedPossessions`'
  subtraction. The finding describes a measured share only - it does not
  claim a positioning, boxing-out, or effort cause. Its bare enum name
  doesn't overclaim (unlike `TooManyThreePointAttempts`), so no Admin-label
  or LLM-prompt substitution was needed for it.
- `PerimeterDefenseProblem` is **retired with no replacement tag**. Its
  trigger (`opponentThreePointPct >= 0.36 && opponent.ThreePointsAttempted >=
  team.ThreePointsAttempted + 5`, hardcoded, bypassing `RulesProfile`
  entirely) read the exact same evidence as `OpponentHotFromThree` -
  opponent three-point percentage and volume - just more loosely: a
  non-tunable threshold and a volume gate satisfiable by as few as 5 opponent
  attempts, far short of a meaningful sample. Its name asserted a tactical
  cause ("perimeter defense") that `TeamStats` cannot establish from a
  shooting percentage alone - the same failure mode as the retired
  `LackOfPaintPressure`/`DefensiveReboundProblem` triggers. Unlike those two,
  it was **not** replaced: `OpponentHotFromThree` already is the correct,
  literal, profile-tunable observation of this same evidence, so reusing the
  ordinal or appending a new tag would only have duplicated it under a
  causally-loaded name. `StatRulesEngine` no longer evaluates
  `PerimeterDefenseProblem` under any input; the enum member and Admin
  mapping are kept for already-persisted records. The audit found no real
  coverage gap from this retirement - the narrow percentage band the old
  rule could reach that `OpponentHotFromThree` cannot was only reachable
  through the old rule's undersized sample gate, so `OpponentHotFromThree`
  was deliberately left unchanged rather than broadened to compensate.
- `HighOpponentEffectiveFieldGoalPercentage` **replaces** `InteriorDefenseProblem`'s
  trigger (`opponentFieldGoalPct >= OpponentHighFieldGoalPct`, raw/unweighted
  FG% across every shot type combined, with **no minimum-attempts gate at
  all** - the weakest sample protection of any rule in `StatRulesEngine`).
  `TeamStats` has no shot-location data to support an "interior defense"
  finding specifically, so the causal name fails the same way as the other
  retirements above. Unlike `PerimeterDefenseProblem`, though, the underlying
  signal - the opponent's overall shooting efficiency - was **not** already
  covered elsewhere (`OpponentHotFromThree` only reads three-point shooting),
  so it was replaced rather than retired outright, mirroring
  `LowDefensiveReboundPercentage`'s and `LowFreeThrowRate`'s pattern instead.
  The new tag (appended at ordinal `16`) reads the opponent's
  `EffectiveFieldGoalPercentage` (M2A via M2B - the same eFG% formula as
  `LowEffectiveFieldGoalPercentage`, crediting three-pointers at 1.5x),
  gated on a real `FieldGoalsAttempted` minimum. `RulesProfile.OpponentHighFieldGoalPct`
  was removed; the new `OpponentHighEffectiveFieldGoalPct` field's five
  per-level values (EasyBasket 0.56, Youth 0.55, Amateur 0.56, Pro 0.59,
  Amateur_Development 0.55) are **newly chosen defaults for eFG%'s scale, not
  a reuse of the old field's numbers**: eFG% runs systematically higher than
  raw FG% for any team with real three-point volume, so keeping the old
  raw-FG%-calibrated numbers unchanged would have made this trigger fire more
  easily than the retired rule did for equivalent-quality shooting. Each new
  value is a deliberate bump above the old field's corresponding value, sized
  by how much three-point volume is realistic at that level - smallest at
  EasyBasket (young players rarely shoot threes), largest at Pro (the
  highest-volume, highest-value three-point shooting) - see
  `Docs/03-domain-and-rules.md` for the full table and rationale.
  `OpponentHighEffectiveFieldGoalPctAttemptsMin` is unchanged from its
  original design and still mirrors `OurLowEffectiveFieldGoalPctAttemptsMin`'s
  per-level attempts minimums.
- `TransitionDefenseProblem` is **retired with no replacement tag**. Its
  trigger (`opponent.Points - team.Points >= LossByPointsToFlagTransition &&
  team.Turnovers >= TurnoversMinToFlagTransition`, both fields removed) read
  only score margin and an absolute team-turnover count. `TeamStats` has no
  fast-break points, points-off-turnovers, live/dead-ball turnover
  distinction, possession-sequencing, or shot-timing data to connect a
  turnover to the opponent scoring off it, let alone in transition
  specifically - a turnover creates an opportunity, not proof of a converted
  possession, and score margin has many causes unrelated to transition
  defense. The only measurable fact the trigger used - elevated team
  turnovers - is already covered by `TurnoverProblem`'s own,
  already-reviewed, differential-based rule (`team.Turnovers -
  opponent.Turnovers >= TurnoverDiffToFlag`), which is unchanged by this
  retirement. Same pattern as `PerimeterDefenseProblem`: the measurable
  content is redundant with an existing finding, so nothing replaces it -
  no renamed turnover- or score-margin-based tag was added.
- `FoulsProblem`'s differential trigger (`team.PersonalFouls -
  opponent.PersonalFouls >= FoulsDiffToFlag`) was reviewed and found
  **directionally sound** - unlike every other rule reviewed in this
  document, a foul count directly supports a foul finding with no inferential
  leap, and it already read no causal or score information. It was found
  **under-inclusive**, though: reading only the gap between two counts means
  a genuinely foul-heavy game on both sides (e.g. 20 fouls to 17, diff 3)
  never fired, however high the absolute total climbed, as long as the
  opponent kept pace. A second, independent condition -
  `FoulsHighCountToFlag`, a plain absolute foul count - was OR'd into the
  same tag to close that gap; this is a strict expansion (everything that
  fired before still fires) refined **in place, same tag, no new enum
  member**, matching the reused-trigger pattern of
  `OffensiveEfficiencyProblem`/`TooManyThreePointAttempts` rather than a
  retirement. `TeamCalculatedMetrics.FoulRate` (M2B, fouls per opponent
  estimated possession) was deliberately **not** adopted: the demonstrated
  gap is fully solved within the count domain, `FoulRate`'s unit is far less
  natural for a coach than a plain foul count, and it would import the same
  non-positive-`EstimatedPossessions` edge case `OffensiveEfficiencyProblem`
  already guards against, for no offsetting benefit. **The differential is
  retained as-is, unchanged, for backward compatibility and because it still
  detects a real relative imbalance - it is NOT normalized by elapsed game
  time or possessions.** Comparing both teams' foul counts at the same
  moment is not the same as accounting for how much of the game that moment
  represents: a five-foul gap after five minutes is not equivalent to a
  five-foul gap after forty, and `StatRulesEngine.Evaluate` has no
  `GameFormat`/`GameTiming` access to tell the two apart (an engine-wide
  limitation, not specific to this rule). The differential still permits an
  early **5-0** result to trigger `FoulsProblem` at Amateur level, exactly as
  before this refinement - this was not changed. Early-live-game confidence
  for this and every other count/rate-based rule remains an open, unresolved
  general concern, not something this refinement closes.
  `FoulsHighCountToFlag`'s five per-level values are explicit project
  defaults chosen for this review, not a universal coaching standard.
- `AnalysisHistoryService.RulesetVersion` was bumped eight times across these
  ten slices (`1.2` -> `1.3` for `LowFreeThrowRate`, `1.3` -> `1.4` for
  `OffensiveEfficiencyProblem`, `1.4` -> `1.5` for `TooManyThreePointAttempts`,
  `1.5` -> `1.6` for `LowDefensiveReboundPercentage`, `1.6` -> `1.7` for
  `PerimeterDefenseProblem`'s retirement, `1.7` -> `1.8` for
  `InteriorDefenseProblem`'s retirement/replacement, `1.8` -> `1.9` for
  `TransitionDefenseProblem`'s retirement, `1.9` -> `1.10` for
  `FoulsProblem`'s refinement) to mark each change.

`OpponentHotFromThree` was reviewed against the same criteria and found
already sound: a measured opponent shooting result gated on a minimum
three-point-attempts sample, reading only opponent data, with no score,
foul, or other causal input, and a coach-facing label/LLM value that were
already plain observations. Only its percentage side was migrated off
`LegacyPercentageBridge` onto the M2A `ThreePointPercentage` already exposed
per-side by `GameCalculatedMetricsCalculator` - an identical formula
(including the same zero-attempts-means-`0.0` convention), so no trigger,
threshold, tag, or `RulesetVersion` change was needed for it.

`OurShootingInefficiency` was reviewed the same way and reached the same
"sound as-is" conclusion, but with one real defect: despite its broad name,
the trigger reads three-point percentage only (`ThreePointsMade`/
`ThreePointsAttempted`), gated on a `3PA` minimum, never overall FG%/eFG% -
"Our Shooting Inefficiency" overclaimed general shooting struggles the data
never measured. Unlike `TooManyThreePointAttempts` (a threshold-judgment
correction) or the retirements above (a data-connection failure), this was a
pure naming defect on an already-correct trigger. Fixed with the same
two-layer wording substitution already established for
`TooManyThreePointAttempts`: the Admin label (`ProblemTagDto`) changed from
"Our Shooting Inefficiency" to "Low Three Point Percentage", and the LLM
prompt value (`OpenAiSuggestionClientHttp.LlmDescription`) now returns "Low
three-point percentage" instead of falling through to the raw enum name.
`ExposesInternalIdentifier` (the leak filter) was **not** changed - it still
checks `tag.ToString()` for `OurShootingInefficiency`, so the raw enum name
is rejected if a model produces it, while the neutral phrase is never
mistaken for a leak - the same separation of "what the prompt sends" from
"what the filter rejects" already in place for `TooManyThreePointAttempts`.
`ProblemTag.OurShootingInefficiency` (ordinal `3`) is unchanged - no new enum
member, since renaming a sound finding does not change what the existing
ordinal means across history. Its percentage side was also migrated off
`LegacyPercentageBridge` onto the M2A `ThreePointPercentage` (an identical
formula), the same data-source migration pattern as `OpponentHotFromThree`.
No trigger, threshold, tag, or `RulesetVersion` change was needed.

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
actually establish. Several remaining tags still claim causes the raw stats
don't fully support - `RulesProfile` itself already labeled the fields behind
the now-retired `InteriorDefenseProblem` and `TransitionDefenseProblem` as
proxies before this review retired both. In particular:
**`EstimatedPace` (M2C) measures tempo, but does not by itself establish
`PaceControlProblem`** - turning a tempo number into a "pace is a problem"
judgment needs a threshold/interpretation layer on top, which is M3 scope,
not something M2 already did.

Two input edge cases surfaced during the M2 review are open questions, not
confirmed defects or completed fixes - worth a decision before M3 relies on
these values:

- Box-score input that passes today's API validation can still produce a
  negative `EstimatedPossessions` (no validation rule relates
  `OffensiveRebounds` to `FieldGoalsAttempted`/`Turnovers`/`FreeThrowsAttempted`).
  The underlying validation policy is still unresolved - but
  `OffensiveEfficiencyProblem` (see above) now explicitly guards against a
  non-positive `EstimatedPossessions` before trusting `OffensiveRating`, as
  one rule-level mitigation, not a fix to the input itself.
- `GameTiming.ElapsedGameTime` has no defensive clamp for a clock value that
  exceeds its period length; the API blocks this via a cross-field validator,
  but a Domain/Application caller that builds `GameTiming` directly is not
  protected the same way.

Live-sample confidence (how much a metric like `EstimatedPace` should be
trusted early in a game, before enough of it has been played) is also still
an open, unaddressed concern - not resolved by M2A/M2B/M2C. `FoulsProblem`'s
review (above) surfaced a concrete instance of the same general concern in
`StatRulesEngine`: its differential trigger still permits an early 5-0 foul
result to fire at Amateur level, because the rule has no access to
`GameFormat`/`GameTiming` and cannot tell an early, thin sample from a
full-game one. That trigger was deliberately left as-is rather than expanded
into timing-aware logic - fixing it properly would mean threading
`GameFormat`/`GameTiming` through `StatRulesEngine.Evaluate` for every rule,
not a single-slice change.

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
