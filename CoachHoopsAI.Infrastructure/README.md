# CoachHoopsAI.Infrastructure

## Purpose

This project contains infrastructure implementations for interfaces defined in the Application layer.

It allows CoachHoopsAI to integrate with external systems without affecting core logic.

---

## Current State

- Provides FakeSuggestionClient implementing ILlmSuggestionClient
- Generates deterministic placeholder suggestions
- Enables full end-to-end execution without real AI dependencies

---

## Responsibilities

- Implement AI clients
- Integrate with external services
- Host future persistence or messaging implementations

---

## What This Project Does NOT Do

- No business rules
- No validation
- No HTTP handling

---

## Dependencies

- CoachHoopsAI.Application
- CoachHoopsAI.Domain

---

## Notes

This project is expected to evolve significantly once real AI providers and persistence are introduced.
