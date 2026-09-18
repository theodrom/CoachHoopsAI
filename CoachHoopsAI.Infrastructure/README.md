# CoachHoopsAI.Infrastructure

## Purpose

This project contains infrastructure implementations for interfaces defined in the Application layer.

It allows CoachHoopsAI to integrate with external systems without affecting core logic.

---

## Current State (V2.0)

- Provides a real OpenAI-backed LLM client using the Responses API
- Enforces structured outputs via JSON Schema
- Uses diagnostics and rule signals as AI grounding
- Filters generated suggestions to prevent internal identifiers (ProblemTag names, rules-profile keys) from leaking into their text - see `Docs/08-ai-integration.md`
- Substitutes a neutral description for `ProblemTag.TooManyThreePointAttempts` specifically before it reaches the prompt, since that bare enum name reads as a resolved verdict ("too many") rather than the coaching-judgment observation it actually is - see `Docs/03-domain-and-rules.md`
- Includes a `FakeSuggestionClient` fallback (used when `Ai:Provider` isn't `OpenAI`) that always returns zero suggestions, so the system can run without a real API key

---

## Responsibilities

- Implement AI clients
- Integrate with external services

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
