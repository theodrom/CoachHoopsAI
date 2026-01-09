# Architecture

CoachHoopsAI follows a strict layered architecture.

## Layers

### API
- HTTP boundary
- Validation (FluentValidation)
- DTOs and mapping
- Error handling

### Application
- Use cases
- Orchestration
- Diagnostics calculation
- Rules profile resolution
- Interfaces for infrastructure

### Domain
- Basketball concepts
- Deterministic rules
- Rule thresholds via RulesProfile
- No infrastructure or configuration binding

### Infrastructure
- AI client implementations
- External integrations
- Fake AI used in V1

## Key Principles
- Deterministic core
- Explainable outputs
- No premature persistence or UI
- AI isolated behind interfaces
