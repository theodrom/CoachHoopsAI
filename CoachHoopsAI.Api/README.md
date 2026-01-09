# CoachHoopsAI.Api

## Purpose

This project exposes the HTTP API for CoachHoopsAI.

It handles request validation, mapping, and response formatting, acting as a clean boundary between clients and application logic.

---

## Responsibilities

- Expose REST endpoints
- Define API request/response DTOs
- Perform validation using FluentValidation
- Map DTOs to application models
- Return RFC 7807–compliant error responses

---

## Endpoints

### POST /api/game-analysis

Accepts:
- Team and opponent statistics
- Competition level
- Optional game metadata (date, team names, competition, location)
- Optional rules profile override

Returns:
- Detected ProblemTags
- Coaching suggestions grouped by category
- Diagnostics explaining stat differences
- Applied rules profile name

---

## Validation

- Manual FluentValidation invocation (v12+)
- Nested validation for Team and Opponent stats
- Invalid requests return `ValidationProblemDetails`
- Invalid requests never reach application logic

---

## What This Project Does NOT Do

- No business logic
- No basketball rules
- No AI calls
- No persistence

---

## Dependencies

- CoachHoopsAI.Application
- CoachHoopsAI.Domain
- CoachHoopsAI.Infrastructure
