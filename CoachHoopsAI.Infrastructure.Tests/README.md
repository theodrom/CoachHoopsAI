# CoachHoopsAI.Infrastructure.Tests

## Purpose

Regression tests for the internal-identifier leak filter in
`OpenAiSuggestionClientHttp` - the check that drops any AI-generated suggestion
whose text or reason contains a raw `ProblemTag` name or rules-profile key
verbatim.

## What's covered

Seven scenarios, all against `GetSuggestionsAsync`: a clean suggestion is
preserved; a suggestion leaking a `ProblemTag` name in its text is removed; the
same in its reason is removed; a suggestion leaking the rules-profile name is
removed; natural basketball language that merely resembles a tag (e.g. "hot
from three", "perimeter defense") survives - only an exact identifier match is
rejected; blank-text/reason filtering still works alongside the identifier
filter; and in a mixed response, only the offending suggestion is dropped while
the clean one is preserved.

## Testing boundary

**Does not call the real OpenAI API.** Tests exercise the real, unmodified
`OpenAiSuggestionClientHttp.GetSuggestionsAsync` against a fake `HttpMessageHandler`
that returns a canned Responses API JSON body - only the observable behavior at
the public method boundary is tested (what "the model" returned in vs. what
`Suggestion`s come out). The prompt text, the real OpenAI service, the Responses
API itself, and JSON (de)serialization internals are not under test.

## Fakes/helpers

Both in `AI/`:

- `FakeResponsesApiHandler` - an `HttpMessageHandler` that always returns the
  same canned body, standing in for the network call
- `ResponsesApiEnvelope` - builds the minimal `output[].content[].{type, text}`
  envelope the client parses, wrapping a `{"suggestions": [...]}` payload

## Running

```
dotnet test CoachHoopsAI.Infrastructure.Tests
```
