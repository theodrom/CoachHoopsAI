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

- Team and opponent statistics (raw box-score counts - made/attempted shooting, rebounds, assists, turnovers, steals, blocks, fouls; see `Docs/02-api-contracts.md`)
- Competition level
- **Game format and game timing (required)** - the structure of the game and where it currently stands
- Optional game metadata (date, team names, competition, **season, location**)
- Optional rules profile override

Returns (`AnalyzeGameResponse`):

- `AnalysisId` (Guid) of the persisted analysis record (V2.1)
- Detected ProblemTags
- Coaching suggestions grouped by category
- Diagnostics explaining stat differences, including the applied rules profile name (nested inside `Diagnostics`, not top-level)

This response does not include Season or Location - those are metadata fields on the stored record, not fields the analysis endpoint echoes back.

Every successful request is persisted via `IAnalysisHistoryService` before the response is returned.

### GET /api/analyses/{id}  *(V2.1)*

Returns the full stored `AnalysisRecord` for the given id (input snapshot, problem tags, diagnostics, suggestions, ruleset/prompt versions, applied profile, timestamps, **season, location**).

Returns `404` if the id does not exist.

### GET /api/analyses  *(V2.1)*

Paged search over historical analyses. Query parameters:

| Name | Type | Notes |
|------|------|-------|
| `teamName` | string | optional filter |
| `level` | string | optional filter (`EasyBasket`, `Youth`, `Amateur`, `Pro`) |
| `tag` | string | optional ProblemTag filter |
| `fromUtc` | DateTime | optional inclusive lower bound on `CreatedUtc` |
| `toUtc` | DateTime | optional inclusive upper bound on `CreatedUtc` |
| `page` | int | defaults to `1` |
| `pageSize` | int | defaults to `20` |

`season` and `location` are **not** query filters. Each returned item does
include **season**, **location**, `problemTagsJson`, and `aiModel` as fields,
populated from what was actually submitted/used for that analysis (`season`/
`location` are `null` when not supplied).

---

## Validation

- Manual FluentValidation invocation (v12+)
- Nested validation for Team, Opponent, GameFormat, and GameTiming
- Invalid requests return `ValidationProblemDetails`
- Invalid requests never reach application logic

---

## What This Project Does NOT Do

- No business logic
- No basketball rules
- No AI calls
- No direct database access (delegated to `CoachHoopsAI.Persistence` via Application interfaces)

---

## Dependencies

- CoachHoopsAI.Application
- CoachHoopsAI.Domain
- CoachHoopsAI.Infrastructure
- CoachHoopsAI.Persistence (registered for DI; not referenced by controllers directly)

---

## Configuration (V2.1)

Requires a SQL Server connection string at `ConnectionStrings:CoachHoopsAI` in `appsettings.json` (or user-secrets / environment). The application fails fast at startup if it is missing.
