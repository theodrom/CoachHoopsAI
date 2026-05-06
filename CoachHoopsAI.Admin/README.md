# CoachHoopsAI.Admin (v3.1.0)

## Purpose


This project provides the Blazor Server admin UI for CoachHoopsAI.

## What's New in 3.1.0

- **New Analysis Form**: Enter basketball game data manually and submit to the API
- **Menu Update**: "Add Analysis" link with custom icon in the sidebar
- **Improved Navigation**: Only one menu item is active at a time

It allows users to:

- Browse and search persisted analyses
- View coaching suggestions and diagnostics
- Trigger new analyses from the UI

---

## Responsibilities

- Display analysis history and details
- Call the API via `AnalysisApiClient`
- Render suggestions, diagnostics, and rules profile info
- Provide a modern, responsive UI using Bootstrap

---

## Key Features

- **Analysis List**: Paginated, filterable list of all analyses
- **Analysis Details**: View problem tags, suggestions, diagnostics, applied rules profile, **season, and location** for a single analysis
- **API Integration**: Uses `IAnalysisApiClient` to call the backend
- **Blazor Server**: Real-time UI updates, no client-side build required

---

## Configuration

- API base URL is set in `appsettings.Development.json` and `appsettings.json`
- Requires the API project (`CoachHoopsAI.Api`) to be running

---

## Launching

- Use the VS Code compound launch config (`Run API + Admin`) to start both API and Admin together
- Admin UI will open automatically at `https://localhost:7071`

---

## Future Improvements

- Edit and manage rules profiles
- User authentication and roles
- Enhanced filtering and export
