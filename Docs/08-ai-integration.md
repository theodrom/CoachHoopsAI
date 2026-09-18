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
- A post-processing filter independently drops any suggestion whose text or reason contains one of the exact raw ProblemTag names or rules-profile name(s) - **the literal enum name, for every tag, regardless of what that tag's prompt value actually was** - so a suggestion can't leak an internal identifier even if the model doesn't follow the instruction, and even if it produces that name from general pattern-matching rather than by copying it from this specific prompt
- `TooManyThreePointAttempts` is a special case, but only for what the *prompt
  sends*, not for what the *filter rejects*. Its bare enum name reads as a
  resolved verdict ("too many") to a model that only has the raw identifier to
  go on, but the finding is a coaching-judgment threshold, not proof a
  different shot mix would score more (see `Docs/03-domain-and-rules.md`). The
  prompt therefore sends the neutral phrase "High three-point share with low
  three-point percentage" for this one `ProblemTag` instead of its literal
  name; every other tag's prompt value is still its bare enum name. The leak
  filter is intentionally **not** changed to match: it still rejects the raw
  `TooManyThreePointAttempts` string if the model produces it (an internal
  identifier leaking, regardless of what was sent), while explicitly allowing
  a suggestion that uses the neutral phrase verbatim (that phrase is the
  sanctioned coaching-language wording for this finding, not a leak - treating
  it as one would discard valid answers for using the wording they were told
  to use). The enum member/ordinal itself is unchanged either way - this
  substitution only affects what the LLM prompt shows, not the API response's
  `problemTags` array or persisted history (see `Docs/02-api-contracts.md`)
- Suggestions with blank text or reason (after trimming) are dropped
- If every suggestion generated for a request ends up filtered out, the call fails rather than returning an empty result
- The fake AI client (used when `Ai:Provider` is not `OpenAI`) always returns zero suggestions - it exists so the system runs without a real API key, not to produce placeholder suggestions

## Future Improvements

- Prompt versioning
- Multi-language support
- Per-level tone tuning
