# CoachHoopsAI.Infrastructure (v3.0.1)

## Purpose

This project contains infrastructure implementations for interfaces defined in the Application layer.

It allows CoachHoopsAI to integrate with external systems without affecting core logic.

---

## Current State (V2.0)

- Provides a real OpenAI-backed LLM client using the Responses API
- Enforces structured outputs via JSON Schema
- Uses diagnostics and rule signals as AI grounding
- Includes a FakeSuggestionClient fallback for development and testing

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

Real AI integration was introduced in V2.0 and persistence (in a dedicated `CoachHoopsAI.Persistence` project) in V2.1. This project remains the home for outbound integrations (AI today, messaging/other external services later).

---

## AI Design Principles (v2.0)

- AI does not make decisions; it provides suggestions
- Deterministic rules remain the source of truth
- Diagnostics are passed explicitly to avoid hallucination
- All AI output must conform to a strict schema
