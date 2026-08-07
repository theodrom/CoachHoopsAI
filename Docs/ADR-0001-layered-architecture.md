# ADR-0001: Layered Architecture

## Context
The system needs to support AI, rules, and future UI/persistence without tight coupling.

## Decision
Adopt a strict layered architecture: API, Application, Domain, Infrastructure.

## Consequences
- Clear separation of concerns
- Easy testing
- Slower initial setup, faster long-term iteration
