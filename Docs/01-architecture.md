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
- Owns `GameFormat` and `GameTiming` (a game's structure and current position - see `Docs/03-domain-and-rules.md`)
- No infrastructure or configuration binding

### Infrastructure

- AI client implementations
- External integrations
- Fake AI client available for development and testing (not V1-only)

### Persistence

- Owns the EF Core `DbContext` and analysis-history entities
- Implements the repository interfaces defined in Application
- Backed by SQL Server

## Key Principles

- Deterministic core
- Explainable outputs
- AI isolated behind interfaces

### AI Integration

- Uses OpenAI Responses API
- Structured outputs enforced via JSON Schema
- AI clients are swappable via DI
- A post-processing filter prevents suggestions from leaking internal identifiers (ProblemTag names, rules-profile keys) into generated text

### Compatibility bridge

`StatRulesEngine`'s thresholds were written against shooting percentages, but `TeamStats` now stores only raw made/attempted counts. A small `LegacyPercentageBridge` in Domain computes the percentages the engine needs on the fly. This is temporary scaffolding, not a general calculated-metrics layer - see `Docs/03-domain-and-rules.md`.
