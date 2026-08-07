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

## Philosophy

Rules represent �coach-agreeable� heuristics, not absolute truth.
They are signals for AI prompting, not final judgments.
