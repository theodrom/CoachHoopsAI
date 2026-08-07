# CoachHoopsAI

CoachHoopsAI is an AI-assisted basketball coaching analysis platform.

The system analyzes team and opponent game statistics, applies configurable basketball rules, detects problem areas, and generates actionable coaching suggestions tailored to the competition context.

The current version (**V3.1.0**) includes:

- deterministic, explainable rule-based analysis
- configurable rule thresholds via Rules Profiles
- real AI-powered coaching suggestions
- structured, schema-validated AI output
- diagnostics used as AI grounding
- strong API validation and regression samples
- **persistence of every analysis (input, diagnostics, tags, suggestions, season, location, AI model) to SQL Server as top-level columns**
- **history retrieval and search endpoints**
- **Blazor Admin UI for browsing analyses, suggestions, and diagnostics**
- game format and game timing captured per analysis, alongside raw box-score inputs (made/attempted counts, not percentages)
- automated unit and integration tests across the Domain, Application, Infrastructure, and API layers

---

## Architecture Overview

The solution follows a layered architecture:

- **API** � HTTP boundary, validation, DTOs, mapping
- **Application** � orchestration, diagnostics, rules profile resolution
- **Domain** � basketball concepts and deterministic rules
- **Infrastructure** � AI and external service implementations

Each layer has a single responsibility and minimal coupling.

---

## Admin UI (V3.1.0)

CoachHoopsAI.Admin is a Blazor Server project providing a modern admin interface:

- Browse/search persisted analyses
- View coaching suggestions, diagnostics, and applied rules profile
- Paginated, filterable history
- Responsive UI (Bootstrap)
- **New in 3.1.0:** Add Analysis form, improved navigation, and custom sidebar icon

### Launching the UI

- Use the VS Code compound launch config (**Run API + Admin**) to start both API and Admin together
- Admin UI opens automatically at `https://localhost:7071`
- API runs at `https://localhost:7294` (no root page)

See [CoachHoopsAI.Admin/README.md](CoachHoopsAI.Admin/README.md) for details.

---

## Key Concepts

### Game Analysis

Input:

- Team stats and opponent stats (raw box-score counts: made/attempted shooting, rebounds, assists, turnovers, steals, blocks, fouls)
- Game format (regulation periods/length, overtime length) and game timing (current period, clock remaining) — both required
- Competition level
- Season (now a top-level field and column)
- Location (now a top-level field and column)
- Optional game metadata
- Optional rules profile override

Output:

- ProblemTags (deterministic)
- Coaching suggestions (AI-generated)
- Diagnostics explaining *why* tags fired
- Applied rules profile

### Rules Profiles (V1.3)

Rules are no longer hardcoded.

- Thresholds are defined in named **Rules Profiles**
- Each Level maps to a default profile
- Requests can override the profile
- Profiles are currently configured via `appsettings.json`

This enables �levels inside levels� (e.g. development vs advanced teams).

### AI Integration (V2.0)

AI suggestions are generated using a real LLM via the OpenAI Responses API.

- Deterministic rules and diagnostics provide grounding
- Suggestions are returned as structured JSON (schema enforced)
- AI is isolated behind an interface and can be swapped or disabled
- A fake AI client remains available for development and testing

### Analysis History (V2.1)

Every analysis request is persisted as an immutable record.

- Stored fields include: input snapshot, applied rules profile, problem tags, diagnostics, AI suggestions, ruleset and prompt versions
- Each `POST /api/game-analysis` response now returns an `AnalysisId`
- History is queryable via `GET /api/analyses/{id}` and `GET /api/analyses` (filter by team, level, tag, date range; paged)
- Backed by SQL Server via the `CoachHoopsAI.Persistence` project (EF Core, with migrations)

---

## Samples

`Docs/samples/` contains regression JSON payloads for:

- common basketball scenarios
- expanded rule coverage
- validation failures

See [Docs/samples/README.md](Docs/samples/README.md) for per-sample details. These samples should be used to verify system behavior after any rule or AI change.

---

## Current Limitations

- No authentication

Authentication and user roles will be introduced in a future version.

---

## Documentation

See [Docs/README.md](Docs/README.md) for the full documentation index.
