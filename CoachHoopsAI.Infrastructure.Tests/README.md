# CoachHoopsAI.Infrastructure.Tests

## Purpose

Regression tests for the internal-identifier leak filter in
`OpenAiSuggestionClientHttp` - the check that drops any AI-generated suggestion
whose text or reason contains a raw `ProblemTag` name or rules-profile key
verbatim.

## What's covered

Ten scenarios, all against `GetSuggestionsAsync`: a clean suggestion is
preserved; a suggestion leaking a `ProblemTag` name in its text is removed; the
same in its reason is removed; a suggestion leaking the rules-profile name is
removed; natural basketball language that merely resembles a tag (e.g. "hot
from three", "perimeter defense") survives - only an exact identifier match is
rejected; blank-text/reason filtering still works alongside the identifier
filter; in a mixed response, only the offending suggestion is dropped while
the clean one is preserved; the prompt sent for `TooManyThreePointAttempts`
carries the neutral description ("High three-point share with low
three-point percentage"), not the raw enum name; a suggestion that uses that
same neutral description verbatim is preserved, not treated as a leak (it is
the sanctioned coaching-language wording for that tag, not an internal
identifier); and a suggestion exposing the raw enum name
`TooManyThreePointAttempts` is still removed even though this request's
prompt never sent that string for that tag - the leak filter checks a static,
code-shaped identifier per tag, independent of what the prompt actually
transmitted for it.

## Testing boundary

**Does not call the real OpenAI API.** Tests exercise the real, unmodified
`OpenAiSuggestionClientHttp.GetSuggestionsAsync` against a fake `HttpMessageHandler`
that returns a canned Responses API JSON body - only the observable behavior at
the public method boundary is tested (what "the model" returned in vs. what
`Suggestion`s come out). The prompt's exact wording, the real OpenAI service,
the Responses API itself, and JSON (de)serialization internals are not under
test - the one exception is `FakeResponsesApiHandler.LastRequestBody`, an
opt-in capture used by a single test to confirm which literal string was sent
for a given `ProblemTag`, without asserting on the rest of the prompt's shape.

## Fakes/helpers

Both in `AI/`:

- `FakeResponsesApiHandler` - an `HttpMessageHandler` that always returns the
  same canned body, standing in for the network call. Also exposes
  `LastRequestBody` (the most recently sent request's content, captured but
  unused by most tests) for the one test that needs to inspect what was sent
- `ResponsesApiEnvelope` - builds the minimal `output[].content[].{type, text}`
  envelope the client parses, wrapping a `{"suggestions": [...]}` payload

## Running

```
dotnet test CoachHoopsAI.Infrastructure.Tests
```
