# AI Integration

## Overview

CoachHoopsAI integrates a real LLM to generate coaching suggestions.
The AI is used only after deterministic analysis has completed.

## Architecture

- AI lives in the Infrastructure layer
- Exposed via ILlmSuggestionClient
- Application orchestrates when AI is called
- Domain remains AI-agnostic

## Prompt Inputs

The AI receives:
- Level
- Requested and applied Rules Profile (name only, not the threshold values)
- Team and opponent raw stats (the same box-score counts submitted in the request)
- Optional game metadata
- Optional coach notes
- ProblemTags (deterministic rule output)
- Diagnostics deltas

These inputs act as grounding signals.

**Current boundaries:** GameFormat and GameTiming are captured on every
analysis (see `Docs/02-api-contracts.md`) but are **not** currently sent to the
AI. Prior/historical analyses are **not** sent - each request is evaluated
independently, with no memory of earlier games.

## Output

- 3-10 coaching suggestions
- Categorized as Offense, Defense, or Other
- Each suggestion includes a reason
- Output is validated against a strict JSON Schema

## Reliability

- Structured Outputs (JSON Schema) prevent parsing errors
- ProblemTags and Diagnostics reduce hallucination by grounding every suggestion's reason
- The prompt instructs the model to never output raw internal identifiers (ProblemTag names such as `TurnoverProblem`, or rules-profile keys such as `Amateur_Default`) in generated text
- A post-processing filter independently drops any suggestion whose text or reason contains one of the exact ProblemTag names or rules-profile name(s) sent for that request, so a suggestion can't leak them even if the model doesn't follow the instruction
- Suggestions with blank text or reason (after trimming) are dropped
- If every suggestion generated for a request ends up filtered out, the call fails rather than returning an empty result
- The fake AI client (used when `Ai:Provider` is not `OpenAI`) always returns zero suggestions - it exists so the system runs without a real API key, not to produce placeholder suggestions

## Future Improvements

- Prompt versioning
- Multi-language support
- Per-level tone tuning
