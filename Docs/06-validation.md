# Validation

Validation occurs at the API boundary.

## Technology
- FluentValidation v12+
- Manual invocation

## Key Properties

- Nested validation for Team, Opponent, GameFormat, and GameTiming
- Invalid requests never reach application logic
- Errors returned as ValidationProblemDetails

## Team / Opponent (raw box-score stats)

- All counts must be `>= 0`
- `FieldGoalsMade <= FieldGoalsAttempted`
- `ThreePointsMade <= ThreePointsAttempted`
- `FreeThrowsMade <= FreeThrowsAttempted`
- `ThreePointsMade <= FieldGoalsMade` (a three-pointer is also a field goal)
- `ThreePointsAttempted <= FieldGoalsAttempted`

## GameFormat

- `RegulationPeriods > 0`
- `RegulationPeriodMinutes > 0`
- `OvertimePeriodMinutes > 0`

## GameTiming

- `CurrentPeriod >= 1`
- `ClockRemainingSeconds >= 0`
- `ClockRemainingSeconds` cannot exceed the applicable period length -
  `RegulationPeriodMinutes` while `CurrentPeriod` is regulation,
  `OvertimePeriodMinutes` once it's overtime. This check needs GameFormat and
  GameTiming together, so it runs at the request level rather than on either
  object's own validator.

## Philosophy

Validation enforces:
- data sanity
- invariant protection

It does not enforce basketball correctness.
