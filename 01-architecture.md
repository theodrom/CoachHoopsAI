# Architecture

CoachHoopsAI follows a strict layered architecture.

## Layers

### API

- HTTP boundary
- Validation (FluentValidation)
- DTOs and mapping
- Error handling

### Admin UI (Blazor)

- Presentation layer for browsing analyses, suggestions, and diagnostics
- Calls API via HTTP client
- Launched with API using VS Code compound config

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

### AI Integration

- Uses OpenAI Responses API
- Structured outputs enforced via JSON Schema
- AI clients are swappable via DI
