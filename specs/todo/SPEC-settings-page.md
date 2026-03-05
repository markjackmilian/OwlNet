# SPEC: Settings Page

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet necessita di una pagina Settings centralizzata per la configurazione globale dell'applicazione. La route `/settings` è già presente nel NavMenu ma la pagina non esiste. Non esiste ancora un'infrastruttura di persistenza per i settings applicativi. Questa spec definisce il contenitore (pagina + infrastruttura di persistenza), mentre i singoli pannelli di configurazione (OpenCode health check, LLM provider) sono definiti in spec dedicate.

## Actors

- **User** — any application user (no role distinction at this time).

## Functional Requirements

1. The system SHALL expose a page accessible at the route `/settings`.
2. The page SHALL have a vertical section layout, where each section is a distinct MudBlazor card with title and description.
3. The system SHALL persist application settings in the database via an `AppSetting` entity with a key-value structure (`Key: string`, `Value: string`), at global level (not per-user).
4. The system SHALL expose an application service (interface in the Application layer) for reading and writing settings, with methods: get a setting by key, get all settings, save/update a setting.
5. The system SHALL create EF Core migrations (both SQLite and SQL Server) for the `AppSetting` entity.
6. The page SHALL show visual feedback (snackbar) when a setting is saved successfully or when saving fails.
7. The page SHALL show a loading state (skeleton or spinner) during the initial retrieval of settings from the database.

## User Flow

1. The user clicks "Settings" in the sidebar navigation menu.
2. The system navigates to `/settings` and shows the page with a loading state.
3. The system loads settings from the database.
4. The page displays the configuration sections (initially: OpenCode Health Check, LLM Provider Config).
5. The user modifies a setting within a section.
6. The system saves the change to the database and shows a confirmation snackbar.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Database unreachable during loading | Show an error message on the page with a "Retry" option |
| Saving a setting fails | Show an error snackbar with a descriptive message; the previous value remains displayed |
| Setting key does not yet exist in DB | The service creates a new record (upsert) |
| Empty or null setting value | Allowed: the system saves an empty string |

## Out of Scope

- Per-user or per-project settings (future).
- Authentication/authorization for settings access (all users can access at this time).
- Import/export of settings.
- Specific panels (OpenCode, LLM) — defined in dedicated specs.

## Acceptance Criteria

- [ ] The `/settings` page is reachable from the NavMenu and loads without errors.
- [ ] The `AppSetting` entity is mapped in the DbContext with migrations for both providers (SQLite + SQL Server).
- [ ] The application service allows CRUD on settings with the Result pattern for error handling.
- [ ] The page shows skeleton/spinner during loading.
- [ ] Save operations show feedback via snackbar.
- [ ] Unit tests cover the application service (happy path + errors).

## Dependencies

- SPEC-app-shell (for the general layout, but the page can work with the current layout as well).

## Open Questions

- None.
