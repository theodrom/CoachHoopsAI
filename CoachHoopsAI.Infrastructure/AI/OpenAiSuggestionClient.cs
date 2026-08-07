using CoachHoopsAI.Application.Interfaces;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProblemTag = CoachHoopsAI.Domain.Enums.ProblemTag;

namespace CoachHoopsAI.Infrastructure.AI
{
    // OpenAiSuggestionClient responsibilities:
    // - Build the system/user prompt from:
    //      - stats
    //      - tags
    //      - notes
    //      - level
    // Call the OpenAI HTTP API
    // Parse JSON back into domain Suggestion objects

    /// <summary>
    /// Real LLM-backed suggestion client using OpenAI Responses API + Structured Outputs (JSON Schema).
    /// Raw HttpClient implementation for maximum compatibility and strict JSON parsing.
    /// </summary>
    public sealed class OpenAiSuggestionClientHttp : ILlmSuggestionClient
    {
        private readonly HttpClient _http;
        private readonly OpenAiOptions _options;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public OpenAiSuggestionClientHttp(IOptions<OpenAiOptions>? options, HttpClient http)
        {
            _http = http;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("OpenAI ApiKey is missing. Configure OpenAI:ApiKey.");
            if (string.IsNullOrWhiteSpace(_options.Model))
                throw new InvalidOperationException("OpenAI:Model is missing.");
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new InvalidOperationException("OpenAI:BaseUrl is missing.");
        }

        public async Task<IReadOnlyCollection<Suggestion>> GetSuggestionsAsync(
            GameAnalysisInput input,
            IReadOnlyCollection<ProblemTag> problemTags,
            GameDiagnostics diagnostics,
            string appliedRulesProfile)
        {
            var system = BuildSystemPrompt(input.Level);
            var user = BuildUserPrompt(input, problemTags, diagnostics, appliedRulesProfile);

            var schema = BuildJsonSchema();

            var payload = new
            {
                model = _options.Model,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = new object[] { new { type = "input_text", text = system } }
                    },
                    new
                    {
                        role = "user",
                        content = new object[] { new { type = "input_text", text = user } }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "Suggestion_response",
                        strict = true,
                        schema = schema
                    }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options?.BaseUrl?.TrimEnd('/')}/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options?.ApiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"OpenAI Responses API call failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }

            var jsonText = ExtractFirstOutputText(body);
            if (string.IsNullOrWhiteSpace(jsonText))
                throw new InvalidOperationException("OpenAI response did not contain output_text to parse.");

            StructuredSuggestions? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<StructuredSuggestions>(jsonText, JsonOpts);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse OpenAI structured output JSON. Raw output_text: {jsonText}", ex);
            }

            if (parsed?.Suggestions == null || parsed.Suggestions.Count == 0)
                throw new InvalidOperationException("OpenAI returned an empty suggestions list.");

            var results = new List<Suggestion>(parsed.Suggestions.Count);
            foreach (var s in parsed.Suggestions)
            {
                var cat = ParseCategory(s.Category);
                var text = (s.Text).Trim();
                var reason = (s.Reason).Trim();

                if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(reason))
                    continue;

                // The system/user prompt tells the model never to echo internal
                // identifiers, but prompt instructions aren't a guarantee - drop any
                // suggestion that leaks one of the exact identifiers this request
                // exposed to the model (a ProblemTag name or a rules-profile key)
                // rather than trusting the model to have obeyed.
                if (ExposesInternalIdentifier(text, problemTags, appliedRulesProfile, input.RulesProfile) ||
                    ExposesInternalIdentifier(reason, problemTags, appliedRulesProfile, input.RulesProfile))
                    continue;

                results.Add(new Suggestion(cat, text, reason));
            }

            return results.Count == 0 ? throw new InvalidOperationException("OpenAI returned suggestions but none were valid after normalization.") : results;
        }

        private static SuggestionCategory ParseCategory(string? category)
        {
            return Enum.TryParse<SuggestionCategory>(category, ignoreCase: true, out var cat) ? cat : SuggestionCategory.Other;
        }

        // Checks only the exact identifiers this request actually put in front of the
        // model (its ProblemTags and rules-profile name(s)) - not every ProblemTag
        // that exists - so natural coaching prose that happens to share a word with an
        // enum name (e.g. "turnover") is never mistaken for a leaked identifier.
        private static bool ExposesInternalIdentifier(
            string generatedText,
            IReadOnlyCollection<ProblemTag> problemTags,
            string? appliedRulesProfile,
            string? requestedRulesProfile)
        {
            foreach (var tag in problemTags)
            {
                if (generatedText.Contains(tag.ToString(), StringComparison.Ordinal))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(appliedRulesProfile) &&
                generatedText.Contains(appliedRulesProfile, StringComparison.Ordinal))
                return true;

            if (!string.IsNullOrWhiteSpace(requestedRulesProfile) &&
                generatedText.Contains(requestedRulesProfile, StringComparison.Ordinal))
                return true;

            return false;
        }

        private static object BuildJsonSchema()
        {
            // IMPORTANT: "enum" is a JSON Schema keyword; in C# use @enum to avoid the reserved word.
            return new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "suggestions" },
                properties = new
                {
                    suggestions = new
                    {
                        type = "array",
                        minItems = 3,
                        maxItems = 10,
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "category", "text", "reason" },
                            properties = new
                            {
                                category = new
                                {
                                    type = "string",
                                    @enum = new[] { "Offense", "Defense", "Other" }
                                },
                                text = new { type = "string", minLength = 8, maxLength = 240 },
                                reason = new { type = "string", minLength = 8, maxLength = 360 }
                            }
                        }
                    }
                }
            };
        }

        private static string BuildSystemPrompt(Level level)
        {
            // Universal hard rules every level must obey. These are restated in the
            // user prompt as well, but the system prompt is the strongest channel.
            const string core =
                "You are an experienced basketball coach helping another coach prepare the next practice and the next game. " +
                "Your job is to produce between 3 and 10 concrete, actionable coaching suggestions for the team described in the user message, " +
                "prioritizing the highest-impact issues. Quality over quantity: it is correct to return fewer suggestions if only a few issues truly matter.\n\n" +
                "Hard rules (apply to every suggestion you produce):\n" +
                "1. ProblemTags and Diagnostics are the primary grounding signals. Every suggestion's \"reason\" must explicitly reference at least one ProblemTag " +
                "or a specific Diagnostics field from the user JSON.\n" +
                "2. Never invent, estimate or round statistics. Only mention numbers that appear verbatim in the provided JSON. " +
                "If a number is not present, do not cite a number.\n" +
                "3. Never produce generic platitudes such as \"play harder\", \"communicate better\", \"stay focused\", \"want it more\", " +
                "\"box out\" with no detail, etc. A suggestion is only acceptable if it names a concrete, observable action: " +
                "a specific drill, a specific defensive coverage, a specific spacing/possession adjustment, or a specific role assignment.\n" +
                "4. Pick the top issues. Do not try to cover every possible weakness.\n" +
                "5. Each suggestion's \"category\" must be exactly one of: \"Offense\", \"Defense\", \"Other\".\n" +
                "6. Output strictly valid JSON that conforms to the provided JSON Schema. No prose, no markdown, no comments, no trailing text.\n" +
                "7. \"problemTags\" and \"rulesProfileApplied\"/\"rulesProfileRequested\" are internal system identifiers (e.g. \"TurnoverProblem\", \"Amateur_Default\"), " +
                "not coaching language. Ground your reasoning in what they mean, but never copy one of these raw identifiers into \"text\" or \"reason\" - " +
                "describe the underlying issue in plain coaching language instead.\n";

            // Level-specific tone and vocabulary guardrails.
            var levelGuide = level switch
            {
                Level.EasyBasket =>
                    "\nLevel guidance (EasyBasket - young children):\n" +
                    "- Use very simple, positive, encouraging language.\n" +
                    "- Focus exclusively on fundamentals: footwork, ball-handling, passing, layups, basic spacing, hustle plays.\n" +
                    "- Forbidden vocabulary: \"pick-and-roll\", \"ICE\", \"drop coverage\", \"weak-side rotation\", \"horns\", \"hedge\", \"switch coverage\", " +
                    "\"closeout\", \"trap\", \"zone\". No tactical jargon at all.\n" +
                    "- Each suggestion should read like a short practice activity a child can immediately understand.",
                Level.Youth =>
                    "\nLevel guidance (Youth):\n" +
                    "- Use simple tactical concepts only: help-side defense, deny the wing, basic pick-and-roll defense, transition lanes, boxing out.\n" +
                    "- Prefer practice-ready drills: name the drill, the setup, and the coaching point.\n" +
                    "- Avoid advanced jargon and avoid coverage acronyms.",
                Level.Amateur =>
                    "\nLevel guidance (Amateur adult teams):\n" +
                    "- Provide practical in-game adjustments: matchups, tempo, set-piece tweaks, rebounding assignments, foul management.\n" +
                    "- Tactical vocabulary is acceptable when briefly grounded (one short clause of explanation).",
                Level.Pro =>
                    "\nLevel guidance (Pro):\n" +
                    "- Concise, high-signal tactical language is expected (coverages, rotations, ATO concepts, lineup considerations).\n" +
                    "- Avoid drill-heavy or instructional phrasing; assume the reader is a professional staff.",
                _ =>
                    "\nLevel guidance: provide practical, actionable adjustments at an amateur level."
            };

            return core + levelGuide;
        }

        private static string BuildUserPrompt(
            GameAnalysisInput input,
            IReadOnlyCollection<ProblemTag> tags,
            GameDiagnostics? diagnostics,
            string? appliedRulesProfile)
        {
            // Build a clean, named JSON payload. Only fields with real content are
            // included; absent data is emitted as null so the model never sees an
            // empty stub it could mistake for a signal.
            var payload = new
            {
                level = input.Level.ToString(),
                rulesProfileRequested = string.IsNullOrWhiteSpace(input.RulesProfile) ? null : input.RulesProfile,
                rulesProfileApplied = string.IsNullOrWhiteSpace(appliedRulesProfile)
                    ? (diagnostics?.AppliedRulesProfile)
                    : appliedRulesProfile,
                metadata = input.Metadata,
                coachNotes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                team = input.Team,
                opponent = input.Opponent,
                problemTags = tags.Select(t => t.ToString()).ToArray(),
                diagnostics = diagnostics is null ? null : new
                {
                    diagnostics.PointsDiff,
                    diagnostics.TurnoversDiff,
                    diagnostics.OffensiveReboundsDiff,
                    diagnostics.DefensiveReboundsDiff,
                    diagnostics.ThreePointPctDiff,
                    diagnostics.ThreePointAttemptsDiff,
                    diagnostics.FoulsDiff,
                    diagnostics.TeamFieldGoalPercentage,
                    diagnostics.OpponentFieldGoalPercentage,
                    diagnostics.FieldGoalPctDiff
                }
            };

            var directive =
                "You will receive a JSON payload describing one basketball game.\n" +
                "Task: produce 3 to 10 concrete coaching suggestions, prioritizing the top issues.\n" +
                "\n" +
                "Grounding requirements:\n" +
                "- Treat \"problemTags\" and \"diagnostics\" as the primary signals. Anchor every suggestion's \"reason\" to at least one of them.\n" +
                "- \"team\", \"opponent\" and \"metadata\" provide context only; do not summarize them back to the coach.\n" +
                "- \"coachNotes\" may surface concerns the stats cannot show; weight them, but do not fabricate stats to support them.\n" +
                "- Tailor wording to \"level\" and to the level guidance in the system message.\n" +
                "- If \"problemTags\" is empty AND \"diagnostics\" shows no clear weakness, return only 3 maintenance-oriented suggestions tied to what is working.\n" +
                "\n" +
                "Output requirements:\n" +
                "- Return ONLY a JSON object that matches the provided schema.\n" +
                "- Each suggestion: \"category\" in {Offense, Defense, Other}, \"text\" is the action, \"reason\" cites the grounding tag/metric.\n" +
                "- No invented numbers. No generic advice. No markdown. No commentary outside the JSON.\n" +
                "- Never repeat a \"problemTags\" entry or a rules-profile key verbatim in \"text\" or \"reason\" - translate it into natural coaching language.\n" +
                "\n" +
                "Game payload:\n";

            return directive + JsonSerializer.Serialize(payload, JsonOpts);
        }

        /// <summary>
        /// Extracts the first "output_text" block from the Responses API JSON payload.
        /// Expected shape includes: output[].content[].{type:"output_text", text:"..."}
        /// </summary>
        private static string ExtractFirstOutputText(string responsesApiJson)
        {
            using var doc = JsonDocument.Parse(responsesApiJson);

            if (!doc.RootElement.TryGetProperty("output", out var outputEl) || outputEl.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var outputItem in outputEl.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var contentItem in contentEl.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var typeEl) &&
                        typeEl.GetString() is { } typeStr &&
                        typeStr.Equals("output_text", StringComparison.OrdinalIgnoreCase) &&
                        contentItem.TryGetProperty("text", out var textEl))
                    {
                        return textEl.GetString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        private sealed class StructuredSuggestions
        {
            public List<SuggestionItem> Suggestions { get; set; } = [];
        }

        private sealed class SuggestionItem
        {
            public string Category { get; set; } = "Other";
            public string Text { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }
    }
}

