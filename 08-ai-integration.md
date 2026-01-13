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
- Applied Rules Profile
- ProblemTags
- Diagnostics deltas
- Optional game metadata
- Optional coach notes

These inputs act as grounding signals.

## Output

- 3–10 coaching suggestions
- Categorized as Offense, Defense, or Other
- Each suggestion includes a reason
- Output is validated against a strict JSON Schema

## Reliability

- Structured Outputs prevent parsing errors
- Diagnostics reduce hallucination
- Fake AI client allows offline development

## Future Improvements

- Prompt versioning
- Multi-language support
- Per-level tone tuning
