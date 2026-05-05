# CoachHoopsAI

CoachHoopsAI is an AI-assisted basketball coaching analysis platform.

The system analyzes team and opponent game statistics, applies configurable basketball rules, detects problem areas, and generates actionable coaching suggestions tailored to the competition context.

The current version (**V2.1**) includes:
- deterministic, explainable rule-based analysis
- configurable rule thresholds via Rules Profiles
- real AI-powered coaching suggestions
- structured, schema-validated AI output
- diagnostics used as AI grounding
- strong API validation and regression samples
- **persistence of every analysis (input, diagnostics, tags, suggestions) to SQL Server**
- **history retrieval and search endpoints**


---

## Architecture Overview

The solution follows a layered architecture:

- **API** – HTTP boundary, validation, DTOs, mapping
- **Application** – orchestration, diagnostics, rules profile resolution
- **Domain** – basketball concepts and deterministic rules
- **Infrastructure** – AI and external service implementations

Each layer has a single responsibility and minimal coupling.

---

## Key Concepts

### Game Analysis
Input:
- Team stats
- Opponent stats
- Competition level
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

This enables “levels inside levels” (e.g. development vs advanced teams).

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

The `/samples` folder contains regression JSON payloads for:
- common basketball scenarios
- expanded rule coverage
- validation failures

These samples should be used to verify system behavior after any rule or AI change.

---

## Current Limitations (Intentional)

- No UI
- No authentication

These will be introduced in later versions once the core analysis stabilizes.
