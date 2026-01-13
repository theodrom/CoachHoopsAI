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
            IReadOnlyCollection<ProblemTag> problemTags)
        {
            var system = BuildSystemPrompt(input.Level);
            var user = BuildUserPrompt(input, problemTags);

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

                results.Add(new Suggestion(cat, text, reason));
            }

            return results.Count == 0 ? throw new InvalidOperationException("OpenAI returned suggestions but none were valid after normalization.") : results;
        }

        private static SuggestionCategory ParseCategory(string? category)
        {
            return Enum.TryParse<SuggestionCategory>(category, ignoreCase: true, out var cat) ? cat : SuggestionCategory.Other;
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

        private static string BuildSystemPrompt(Level level) => level switch
        {
            Level.EasyBasket =>
                "You are a youth basketball coach (EasyBasket). Keep advice simple, positive, and drill-oriented. Avoid tactical jargon.",
            Level.Youth =>
                "You are a youth basketball coach. Provide practical adjustments for the next practice and next game.",
            Level.Amateur =>
                "You are a basketball coach for amateur adult teams. Provide actionable coaching suggestions across offense, defense, and habits.",
            Level.Pro =>
                "You are a professional basketball coach. Provide concise, high-signal tactical adjustments when appropriate.",
            _ =>
                "You are a basketball coach. Provide practical, actionable adjustments."
        };

        private static string BuildUserPrompt(GameAnalysisInput input, IReadOnlyCollection<ProblemTag> tags)
        {
            var payload = new
            {
                level = input.Level.ToString(),
                rulesProfileRequested = input.RulesProfile,
                metadata = input.Metadata,
                notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                team = input.Team,
                opponent = input.Opponent,
                problemTags = tags.Select(t => t.ToString()).ToArray(),
                diagnostics = new
                {
                    // Later include diagnostics object here
                }
            };

            return
                "You will receive a JSON payload describing a basketball game.\n" +
                "Task: generate 3–10 specific coaching suggestions.\n" +
                "Rules:\n" +
                "- Use ProblemTags as the primary signals.\n" +
                "- Tailor advice to the Level.\n" +
                "- Keep suggestions actionable (what to do in the next practice or next game).\n" +
                "- Avoid generic advice.\n" +
                "- Do not invent stats.\n" +
                "Return ONLY JSON that matches the provided schema.\n\n" +
                JsonSerializer.Serialize(payload, JsonOpts);
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

