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
/api/game-analysis` response above. See the known persistence gap noted under
`GET /api/analyses` - it applies here too, since both endpoints read the same
stored record.

Returns `404` if the id does not exist.

## GET /api/analyses

Paged search over historical analyses. Query parameters: `teamName`, `level`,
`tag`, `fromUtc`, `toUtc`, `page` (default `1`), `pageSize` (default `20`).

`season` and `location` are **not** currently query filters, even though they
are returned on each item (see below).

Returns `{ page, pageSize, total, items: [...] }`. Each item: `id`,
`createdUtc`, `level`, `appliedRulesProfile`, `gameDate`, `teamName`,
`opponentName`, `season`, `location`, `problemTagsJson`, `aiModel`.

**Known gap:** `AnalysisHistoryService` (the write path) does not currently
copy `Season`/`Location` from the request into the persisted record, and
`AiModel` is always stored as an empty string. All three fields exist on the
schema and are returned here, but currently come back `null`/`""` for every
analysis regardless of what was submitted or which AI provider ran.

Example item (reflecting current behavior):

```json
{
  "id": "...",
  "createdUtc": "...",
  "level": "...",
  "appliedRulesProfile": "...",
  "gameDate": "...",
  "teamName": "...",
  "opponentName": "...",
  "season": null,
  "location": null,
  "problemTagsJson": "[1,3,7]",
  "aiModel": ""
}
```

## Error Handling

Invalid requests return:

- HTTP 400
- ValidationProblemDetails (RFC 7807)
- Field-level errors

See `Docs/06-validation.md` for the validation rules themselves.
