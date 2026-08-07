# CoachHoopsAI.Application

## Purpose

This project contains application-level orchestration and use cases.

It coordinates rule evaluation, diagnostics calculation, rules profile resolution, and AI suggestion generation without depending on HTTP or infrastructure details.

---

## Responsibilities

- Define application use cases
- Resolve rules profiles based on level and overrides
- Orchestrate rule evaluation
- Calculate diagnostics
- Define application models
- Expose interfaces for infrastructure implementations
- Orchestrate persistence of analysis history (V2.1)

---

## Key Components

### Services

- GameAnalysisService
- AnalysisHistoryService *(V2.1)* — runs analysis then persists an immutable `AnalysisRecord`, stamping `RulesetVersion` and `PromptVersion`. Known gap: does not currently copy `Season`/`Location` from the request metadata onto the record, and always stamps `AiModel` as an empty string (see `CoachHoopsAI.Persistence/README.md`)

### Rules

- IRulesProfileProvider
- RulesProfileProvider
- RulesProfilesOptions

### Models

- GameAnalysisInput - also carries `GameFormat`/`GameTiming` as pass-through facts (not yet consumed by rule evaluation, diagnostics, or the AI prompt)
- GameAnalysisResult
- GameDiagnostics
- GameMetadata
- AnalysisRecord *(V2.1)*
- AnalysisRecordListItem *(V2.1)*
- AnalysisSearchQuery *(V2.1)*
- PagedResult&lt;T&gt; *(V2.1)*

### Interfaces

- IGameAnalysisService
- ILlmSuggestionClient
- IAnalysisHistoryService *(V2.1)*
- IAnalysisRepository *(V2.1)* — implemented by `CoachHoopsAI.Persistence`

---

## Design Principles

- No dependency on ASP.NET
- No DTOs
- No configuration binding in Domain
- Fully testable with mocks/fakes

---

## Dependencies

- Depends on CoachHoopsAI.Domain
