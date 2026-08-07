# CoachHoopsAI Documentation

Index of the documentation in this folder. This page only links to and briefly
describes each document — see the linked pages themselves for actual content.

## Reading order

1. [00-overview.md](00-overview.md)
2. [01-architecture.md](01-architecture.md)
3. [02-api-contracts.md](02-api-contracts.md)
4. [03-domain-and-rules.md](03-domain-and-rules.md)
5. [04-rules-profiles.md](04-rules-profiles.md)
6. [05-diagnostics.md](05-diagnostics.md)
7. [06-validation.md](06-validation.md)
8. [08-ai-integration.md](08-ai-integration.md)
9. [09-testing.md](09-testing.md)
10. [samples/README.md](samples/README.md)
11. ADRs: [0001](ADR-0001-layered-architecture.md), [0002](ADR-0002-deterministic-rules-first.md), [0003](ADR-0003-configurable-rules-profiles.md), [0004](ADR-0004-game-format-timing-and-raw-stats.md)
12. Historical version snapshots: [v1-app-basics.md](v1-app-basics.md), [v2-ai-integration(current).md](v2-ai-integration(current).md), [v3-admin-ui.md](v3-admin-ui.md)

## Every document

| Document | Description |
|---|---|
| [00-overview.md](00-overview.md) | Project pitch and phase list |
| [01-architecture.md](01-architecture.md) | Layer-by-layer responsibility summary |
| [02-api-contracts.md](02-api-contracts.md) | `POST /api/game-analysis` request/response fields |
| [03-domain-and-rules.md](03-domain-and-rules.md) | Domain concepts and the rules engine |
| [04-rules-profiles.md](04-rules-profiles.md) | Rules Profiles rationale and mechanics |
| [05-diagnostics.md](05-diagnostics.md) | `GameDiagnostics` fields and purpose |
| [06-validation.md](06-validation.md) | Validation approach and technology |
| [08-ai-integration.md](08-ai-integration.md) | LLM architecture, prompt inputs/outputs, reliability |
| [09-testing.md](09-testing.md) | Test projects, current total, unit/boundary distinction, how to run |
| [samples/README.md](samples/README.md) | Regression sample payloads: purpose and per-file expectations |
| [ADR-0001-layered-architecture.md](ADR-0001-layered-architecture.md) | Decision record: layered architecture |
| [ADR-0002-deterministic-rules-first.md](ADR-0002-deterministic-rules-first.md) | Decision record: rules before AI |
| [ADR-0003-configurable-rules-profiles.md](ADR-0003-configurable-rules-profiles.md) | Decision record: externalized thresholds |
| [ADR-0004-game-format-timing-and-raw-stats.md](ADR-0004-game-format-timing-and-raw-stats.md) | Decision record: GameFormat/GameTiming, raw TeamStats, LegacyPercentageBridge |
| [v1-app-basics.md](v1-app-basics.md) | Historical snapshot: V1 feature checklist |
| [v2-ai-integration(current).md](v2-ai-integration(current).md) | Historical snapshot: V2 feature checklist |
| [v3-admin-ui.md](v3-admin-ui.md) | Historical snapshot: V3 Admin UI feature checklist |

## Project READMEs

| Project | README |
|---|---|
| CoachHoopsAI.Api | [../CoachHoopsAI.Api/README.md](../CoachHoopsAI.Api/README.md) |
| CoachHoopsAI.Application | [../CoachHoopsAI.Application/README.md](../CoachHoopsAI.Application/README.md) |
| CoachHoopsAI.Domain | [../CoachHoopsAI.Domain/README.md](../CoachHoopsAI.Domain/README.md) |
| CoachHoopsAI.Infrastructure | [../CoachHoopsAI.Infrastructure/README.md](../CoachHoopsAI.Infrastructure/README.md) |
| CoachHoopsAI.Persistence | [../CoachHoopsAI.Persistence/README.md](../CoachHoopsAI.Persistence/README.md) |
| CoachHoopsAI.Admin | [../CoachHoopsAI.Admin/README.md](../CoachHoopsAI.Admin/README.md) |

## Test project READMEs

See [09-testing.md](09-testing.md) for the testing overview.

| Project | README |
|---|---|
| CoachHoopsAI.Domain.Tests | [../CoachHoopsAI.Domain.Tests/README.md](../CoachHoopsAI.Domain.Tests/README.md) |
| CoachHoopsAI.Application.Tests | [../CoachHoopsAI.Application.Tests/README.md](../CoachHoopsAI.Application.Tests/README.md) |
| CoachHoopsAI.Api.Tests | [../CoachHoopsAI.Api.Tests/README.md](../CoachHoopsAI.Api.Tests/README.md) |
| CoachHoopsAI.Infrastructure.Tests | [../CoachHoopsAI.Infrastructure.Tests/README.md](../CoachHoopsAI.Infrastructure.Tests/README.md) |
