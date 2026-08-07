# CoachHoopsAI � Project Overview

CoachHoopsAI is an AI-assisted basketball coaching analysis platform.

It analyzes game statistics, applies deterministic basketball rules, and generates actionable coaching suggestions tailored to competition context.

The project is intentionally built in phases:

- deterministic analysis first
- explainability and diagnostics
- configurability of rules
- AI integration
- persistence
- **Admin UI (Blazor) for browsing analyses and suggestions**
- game format, game timing, and raw box-score statistics (replacing pre-computed shooting percentages)
- automated testing (unit/integration test projects for Domain, Application, Infrastructure, and API)

## Admin UI

CoachHoopsAI.Admin is a Blazor Server project for browsing analysis history, suggestions, and diagnostics. Launch both API and Admin together using the VS Code compound config (**Run API + Admin**).

V2.0 introduces real AI integration with structured, explainable outputs.

See the root [README.md](../README.md) for the current product version.
