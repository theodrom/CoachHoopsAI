# ADR-0004: Game Format, Game Timing, and Raw Team Statistics

## Context

Analyses needed to represent the actual structure of a specific game (how many
periods, how long they run) and where the game currently stands (period,
clock), independently of the competition Level used to select rule
thresholds. Team statistics also needed to record what was actually observed
in a game - made/attempted counts - rather than precomputed shooting
percentages that callers could get wrong or that couldn't be reconciled with
a live box score.

## Decision

- `Level` (EasyBasket/Youth/Amateur/Pro) and `GameFormat` are independent
  concepts. `Level` selects the rules profile; `GameFormat` describes the
  structure of the game being played. `GameFormat` must never be inferred
  from `Level` - a Youth game may be played 4x10 or 4x5, and nothing in the
  domain is allowed to assume otherwise.
- `GameTiming` (current period, clock remaining) is explicitly supplied as
  input, not derived or defaulted.
- Regulation periods are numbered `1..N` where `N = GameFormat.RegulationPeriods`.
  Overtime periods continue the same sequence: `N+1` is OT1, `N+2` is OT2, and
  so on. There is no separate "is overtime" input flag - overtime state is
  always derived from `CurrentPeriod` and `RegulationPeriods`, so it can never
  contradict them.
- `TeamStats` stores raw made/attempted/count values (field goals, three
  points, free throws, rebounds, assists, turnovers, steals, blocks, fouls)
  instead of precomputed shooting percentages.
- The existing rules engine and diagnostics calculator were written against
  shooting percentages. They continue to work, unchanged, by computing those
  percentages from the raw counts through `LegacyPercentageBridge`.
- `LegacyPercentageBridge` is temporary compatibility scaffolding, not a
  permanent architectural layer. It exists only to keep current rule
  thresholds and diagnostics working against the new raw-stat model. It
  should be removed once the rules engine and diagnostics are redesigned to
  consume raw counts directly.
- Backward compatibility with old local-development persisted analysis
  snapshots was explicitly not required for this contract change. The
  application was still local/in development, with no external consumers
  depending on the previous percentage-based shape.

## Consequences

- Domain models (`GameFormat`, `GameTiming`, `TeamStats`) represent facts
  observed about a game; derived basketball metrics are calculated elsewhere,
  not stored as input.
- Existing rule thresholds and diagnostics fields did not need to change
  immediately, at the cost of a temporary, explicitly-labeled compatibility
  layer that must eventually be removed.
- API/Admin contracts and local persisted snapshots from before this change
  are not compatible with the new shape; no migration or dual-read path was
  built for them.
