# CoachHoopsAI.Application

## Purpose

This project contains application-level orchestration and use cases.

It coordinates rule evaluation, diagnostics calculation, rules profile resolution, and AI suggestion generation without depending on HTTP or infrastructure details.

---

## Responsibilities

- Define application use cases
- Resolve rules profiles based on level and overrides
- Orchestrate rule evaluation
- Calculate diagnostics
- Define application models
- Expose interfaces for infrastructure implementations

---

## Key Components

### Services
- GameAnalysisService

### Rules
- IRulesProfileProvider
- RulesProfileProvider
- RulesProfilesOptions

### Models
- GameAnalysisInput
- GameAnalysisResult
- GameDiagnostics
- GameMetadata

### Interfaces
- IGameAnalysisService
- ILlmSuggestionClient

---

## Design Principles

- No dependency on ASP.NET
- No DTOs
- No configuration binding in Domain
- Fully testable with mocks/fakes

---

## Dependencies

- Depends on CoachHoopsAI.Domain
