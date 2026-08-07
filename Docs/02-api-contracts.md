# API Contracts

## POST /api/game-analysis

Runs an analysis and persists it. There is no "preview without saving" option -
every valid request is stored.

### Request fields

Required:

- `team` (TeamStats - see below)
- `opponent` (TeamStats)
- `level` (`EasyBasket`, `Youth`, `Amateur`, `Pro`)
- `gameFormat` (GameFormat - see below)
- `gameTiming` (GameTiming - see below)

Optional:

- `notes`
- `gameDate`
- `teamName`
- `opponentName`
- `competition`
- `season`
- `location`
- `rulesProfile` (string override; defaults to the level's default profile)

### TeamStats (`team` / `opponent`)

Raw box-score counts. Shooting percentages are **not** accepted as input - they
are calculated server-side from made/attempted counts where needed.

- `points`
- `fieldGoalsMade`, `fieldGoalsAttempted`
- `threePointsMade`, `threePointsAttempted`
- `freeThrowsMade`, `freeThrowsAttempted`
- `offensiveRebounds`, `defensiveRebounds`
- `assists`, `turnovers`
- `steals`, `blocks`
- `personalFouls`

### GameFormat

- `regulationPeriods`
- `regulationPeriodMinutes`
- `overtimePeriodMinutes`
- `name` (optional, descriptive only - never affects calculations)

### GameTiming

- `currentPeriod` (sequential: `1..regulationPeriods` is regulation, beyond that is overtime)
- `clockRemainingSeconds`

### Response fields (`AnalyzeGameResponse`)

- `analysisId` (Guid of the persisted record)
- `problemTags` (array of strings, e.g. `["TurnoverProblem", "FoulsProblem"]`)
- `suggestions`: `{ offense: [...], defense: [...], other: [...] }`, each item `{ suggestion, reason }`
- `diagnostics`: `{ pointsDiff, turnoversDiff, offensiveReboundsDiff, defensiveReboundsDiff, threePointPctDiff, threePointAttemptsDiff, foulsDiff, teamFieldGoalPercentage, opponentFieldGoalPercentage, fieldGoalPctDiff, appliedRulesProfile }`

`appliedRulesProfile` lives **inside** `diagnostics`, not as a separate top-level
response field.

## GET /api/analyses/{id}

Returns the full stored analysis record: `id`, `createdUtc`, `level`,
`requestedRulesProfile`, `appliedRulesProfile`, `gameDate`, `teamName`,
`opponentName`, `season`, `location`, `rulesetVersion`, `promptVersion`,
`aiModel`, `inputJson`, `problemTagsJson`, `diagnosticsJson`, `suggestionsJson`.

`problemTagsJson` and `aiModel` exist on this stored-record shape and on the
list/search endpoint below - they are **not** part of the `POST
/api/game-analysis` response above.

Returns `404` if the id does not exist.

## GET /api/analyses

Paged search over historical analyses. Query parameters: `teamName`, `level`,
`tag`, `fromUtc`, `toUtc`, `page` (default `1`), `pageSize` (default `20`).

`season` and `location` are **not** currently query filters, even though they
are returned on each item (see below).

Returns `{ page, pageSize, total, items: [...] }`. Each item: `id`,
`createdUtc`, `level`, `appliedRulesProfile`, `gameDate`, `teamName`,
`opponentName`, `season`, `location`, `problemTagsJson`, `aiModel`.

`season`/`location` are populated from the request's metadata when supplied,
and `null` when not (they are never defaulted to a value). `aiModel` is
populated from the identifier the LLM client that produced the suggestions
actually reports - `"Fake"` when the fake client is active, otherwise the
configured OpenAI model name.

Example item:

```json
{
  "id": "...",
  "createdUtc": "...",
  "level": "...",
  "appliedRulesProfile": "...",
  "gameDate": "...",
  "teamName": "...",
  "opponentName": "...",
  "season": "2025/2026",
  "location": "Home Arena",
  "problemTagsJson": "[1,3,7]",
  "aiModel": "gpt-4.1-mini"
}
```

## Error Handling

Invalid requests return:

- HTTP 400
- ValidationProblemDetails (RFC 7807)
- Field-level errors

See `Docs/06-validation.md` for the validation rules themselves.
