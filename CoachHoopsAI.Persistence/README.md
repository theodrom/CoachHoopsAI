# CoachHoopsAI.Persistence (v3.0.1)

## Purpose

This project provides the data persistence layer for CoachHoopsAI.

It implements the storage contracts defined by the Application layer, using **Entity Framework Core** against **SQL Server**.

Introduced in **V2.1**.

---

## Responsibilities

- Define the EF Core `DbContext` (`CoachHoopsAIDbContext`)
- Map application records to persistence entities (`AnalysisRecordEntity`)
- Implement `IAnalysisRepository` (`AnalysisRepository`)
- Own EF Core migrations for the CoachHoopsAI database

---

## Key Components

### DbContext

- `CoachHoopsAIDbContext` � exposes `Analyses` (`DbSet<AnalysisRecordEntity>`), with indexes on `CreatedUtc`, `TeamName`, `GameDate`

### Entities

- `AnalysisRecordEntity` � immutable row capturing a single analysis: input JSON snapshot, problem tags JSON, diagnostics JSON, suggestions JSON, applied/requested rules profiles, ruleset and prompt versions, AI model, **season, location** (top-level columns as of V3.1)

### Repositories

- `AnalysisRepository` � `SaveAsync`, `GetByIdAsync`, `SearchAsync` with paging and optional filters (team, level, tag, date range, **season, location**)

### Migrations

- `20260220131412_InitialSqlServer` � creates the `Analyses` table and indexes
- `20260505154155_AddSeasonAndLocationToAnalysisRecord` � adds `Season` and `Location` columns to `Analyses`

---

## Configuration

Requires a connection string at `ConnectionStrings:CoachHoopsAI` (configured in the API host). Example:

```json
"ConnectionStrings": {
  "CoachHoopsAI": "Server=localhost;Database=CoachHoopsAI;Trusted_Connection=True;TrustServerCertificate=True"
}
```

To create or update the database:

```powershell
dotnet ef database update --project CoachHoopsAI.Persistence --startup-project CoachHoopsAI.Api
```

---

## Design Principles

- No business logic; storage only
- Application code depends on `IAnalysisRepository`, never on EF Core types
- Snapshots (input, tags, diagnostics, suggestions) are stored as serialized JSON for full auditability
- Records are append-only (never mutated after write)

---

## What This Project Does NOT Do

- No HTTP handling
- No validation
- No basketball rules
- No AI calls

---

## Dependencies

- CoachHoopsAI.Application (for `IAnalysisRepository` and models)
- Microsoft.EntityFrameworkCore.SqlServer
