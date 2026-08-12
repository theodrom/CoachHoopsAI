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

- TeamCalculatedMetrics, CalculatedMetricsCalculator (Milestone 2A) - core
  numerical metrics derived from a single `TeamStats`; no production consumer
  yet, see `Docs/03-domain-and-rules.md`

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
