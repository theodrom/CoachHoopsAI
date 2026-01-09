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

- problemTags
- suggestions (grouped)
- diagnostics
- appliedRulesProfile

## Error Handling

Invalid requests return:
- HTTP 400
- ValidationProblemDetails (RFC 7807)
- Field-level errors
