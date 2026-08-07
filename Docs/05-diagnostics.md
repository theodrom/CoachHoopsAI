# Diagnostics

Diagnostics explain *why* problem tags were triggered.

## Current Diagnostics Fields

- PointsDiff
- TurnoversDiff
- OffensiveReboundsDiff
- DefensiveReboundsDiff
- TeamFieldGoalPercentage
- OpponentFieldGoalPercentage
- FieldGoalPctDiff
- ThreePointPctDiff
- ThreePointAttemptsDiff
- FoulsDiff
- AppliedRulesProfile

`TeamFieldGoalPercentage` and `OpponentFieldGoalPercentage` (and the shooting
percentages diagnostics are diffed from) are computed from raw made/attempted
counts at analysis time - they are not stored or submitted as percentages.

## Purpose

Diagnostics are:
- for developers tuning rules
- for future UI explainability
- for AI prompt grounding

They are not meant to be exhaustive analytics.
