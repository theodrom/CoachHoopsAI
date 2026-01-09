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

### Entities
- TeamStats
- Suggestion

### Enums
- Level
- ProblemTag
- SuggestionCategory

### Rules
- IStatRulesEngine
- StatRulesEngine
- RulesProfile

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
