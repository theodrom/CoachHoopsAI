# API Contracts

## Endpoint

POST /api/game-analysis

## Request Fields

Required:

- team (TeamStats)
- opponent (TeamStats)
- level (EasyBasket, Youth, Amateur, Pro)

Optional:

- notes
- gameDate
- teamName
- opponentName
- competition
- season
- location
- rulesProfile (string override)

## Response Fields

- suggestions (grouped)
- diagnostics
- appliedRulesProfile
- problemTagsJson
- aiModel

## Error Handling

Invalid requests return:

- HTTP 400
- ValidationProblemDetails (RFC 7807)
- Field-level errors

### Update (2026-05-07)

- The `AnalysisRecordListItem` model returned by `GET /api/analyses` now includes a `problemTagsJson` property.
- `problemTagsJson` is a JSON array of integers representing the problem tags detected for each analysis (e.g., `[1,3,7]`).
- This enables the admin UI and API consumers to display problem tags per analysis in the history/search list view.
- Example response item:

```json
{
  "id": "...",
  "createdUtc": "...",
  "level": "...",
  "appliedRulesProfile": "...",
  "gameDate": "...",
  "teamName": "...",
  "opponentName": "...",
  "season": "...",
  "location": "...",
  "problemTagsJson": "[1,3,7]"
}
```

- The `aiModel` property was also added to `AnalysisRecordListItem` and is now returned by the API for each analysis record. This field indicates which AI model (if any) was used for the analysis.
- Example response item (with both new fields):

```json
{
  "id": "...",
  "createdUtc": "...",
  "level": "...",
  "appliedRulesProfile": "...",
  "gameDate": "...",
  "teamName": "...",
  "opponentName": "...",
  "season": "...",
  "location": "...",
  "problemTagsJson": "[1,3,7]",
  "aiModel": "gpt-4"
}
```

> Note: This property was added to support richer UI filtering and display of problem tags in the analysis history.
