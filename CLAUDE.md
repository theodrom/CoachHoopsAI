# CLAUDE.md

Repository-specific instructions for AI-assisted development on CoachHoopsAI.

## Before making changes

1. Read the root [README.md](README.md) first.
2. Read [Docs/README.md](Docs/README.md) for the documentation index.
3. Read the documentation under `Docs/` relevant to the subsystem being changed.
4. Read the README of the project being changed (`CoachHoopsAI.<Project>/README.md`).

## Source of truth

Treat the current code, together with the documentation under `Docs/` and the
project READMEs, as the source of truth for implemented behavior. Historical
milestone documents (`Docs/v1-app-basics.md`, `Docs/v2-ai-integration(current).md`,
`Docs/v3-admin-ui.md`) describe what shipped at a point in time - do not treat
them as descriptions of current architecture, and do not rewrite them to look
like they always described today's system.

## Architecture rules

- Preserve the Clean Architecture layer boundaries (API -> Application ->
  Domain; Infrastructure and Persistence implement Application interfaces).
  See `Docs/01-architecture.md`.
- `GameFormat` must never be inferred from `Level` - they are independent
  concepts. See `Docs/ADR-0004-game-format-timing-and-raw-stats.md`.
- `LegacyPercentageBridge` (`CoachHoopsAI.Domain.Compatibility`) is temporary
  compatibility scaffolding, not a permanent architectural layer - do not
  extend it or build new features on top of it.

## Workflow

- Run the relevant test project(s) after code changes (`dotnet test
  CoachHoopsAI.<Project>.Tests`, or `dotnet test CoachHoopsAI.sln` for
  everything) - see `Docs/09-testing.md`.
- When a change materially affects behavior or a contract (API request/response
  shape, validation rules, domain model), update the relevant documentation in
  the same change.
