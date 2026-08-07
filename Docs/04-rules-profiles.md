# Rules Profiles

## Motivation

Not all teams at the same level can or should be judged by the same thresholds.

Rules Profiles allow:
- “levels inside levels”
- development vs advanced teams
- future per-team customization

## Current Implementation (V1.3)

- Rules profiles defined in appsettings.json
- Each Level maps to a default profile
- Requests may override profile by name
- Applied profile is returned in diagnostics

## Example Profiles

- EasyBasket_Default
- Youth_Default
- Amateur_Default
- Amateur_Development
- Pro_Default

## Future (V2)

- Persist profiles
- UI for editing
- Versioning and audit
