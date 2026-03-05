# SPEC: LLM Provider Configuration

> **Status:** Todo
> **Created:** 2026-03-05
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet needs to communicate with an LLM for its features. Configuring the LLM provider is a prerequisite. Initially, only OpenRouter is supported as a provider. The user must be able to enter their API key and specify the model to use. The API key must be saved in encrypted/obfuscated form in the database, not in plain text.

## Actors

- **User** — any application user.
- **OpenRouter API** — external service for API key verification.

## Functional Requirements

1. The system SHALL display a dedicated "LLM Provider" section in the Settings page as a MudBlazor card.
2. The section SHALL contain a text field for entering the OpenRouter API key.
3. The API key SHALL be displayed in password mode (obfuscated with asterisks/dots) with a toggle to show/hide the value.
4. The section SHALL contain a text field for manual entry of the Model ID (e.g., `anthropic/claude-sonnet-4`, `openai/gpt-4o`).
5. The Model ID field SHALL have helper text indicating the expected format and an example.
6. The system SHALL validate that the API key is not empty before allowing save.
7. The system SHALL validate that the Model ID is not empty before allowing save.
8. The section SHALL contain a "Verify Connection" button that performs a `GET https://openrouter.ai/api/v1/models` call with the entered API key in the `Authorization: Bearer <key>` header.
9. If the verification succeeds (HTTP 200), the system SHALL display a green badge/chip "Connection verified".
10. If the verification fails (HTTP 401, 403, network error, timeout), the system SHALL display a red badge/chip with a descriptive error message.
11. The system SHALL encrypt the API key before saving it to the database using ASP.NET Core Data Protection API (`IDataProtector`).
12. The system SHALL decrypt the API key when reading it from the database to display it (obfuscated) in the interface or to use it in API calls.
13. The section SHALL contain a "Save" button that persists the API key (encrypted) and Model ID to global settings.
14. On section load, the system SHALL retrieve saved values from the database and populate the fields (API key decrypted but displayed obfuscated, Model ID in plain text).
15. The system SHALL display the current configuration status: a "Configured" badge (green) if API key and Model ID are present, "Not configured" badge (orange) if missing.

## User Flow

### Happy Path — First-time configuration
1. The user opens the Settings page.
2. The LLM Provider section shows an orange badge "Not configured".
3. The user enters the OpenRouter API key in the dedicated field.
4. The user enters the Model ID (e.g., `anthropic/claude-sonnet-4`).
5. The user clicks "Verify Connection".
6. The system calls OpenRouter: success. Green badge "Connection verified".
7. The user clicks "Save".
8. The system encrypts the API key, saves both values to the database. Confirmation snackbar.
9. The status badge updates to "Configured" (green).

### Editing existing configuration
1. The user opens the Settings page.
2. The LLM Provider section shows a green badge "Configured", pre-filled fields (API key obfuscated, Model ID visible).
3. The user modifies the Model ID.
4. The user clicks "Save". Save confirmed.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty API key on save | Inline validation: message "API key is required", save blocked |
| Empty Model ID on save | Inline validation: message "Model ID is required", save blocked |
| Verify connection with empty API key | Button disabled when the API key field is empty |
| Verification returns HTTP 401 | Red badge "Invalid API key" |
| Verification returns HTTP 403 | Red badge "Access denied. Check API key permissions." |
| Verification fails due to network error | Red badge "Unable to reach OpenRouter. Check your internet connection." |
| Verification times out (>15s) | Red badge "Timeout. Try again later." |
| Saved API key cannot be decrypted (protection key changed) | Show empty field with warning "The saved API key is no longer readable. Please enter a new one." |
| Database save fails | Error snackbar, form values unchanged |

## Out of Scope

- Support for providers other than OpenRouter (future).
- Model selection from a list loaded via API (future: the `/api/v1/models` endpoint could be used to populate a dropdown).
- Configuration of model parameters (temperature, max tokens, etc.).
- Per-project LLM provider configuration.
- Actual use of the API key for LLM calls (separate spec).

## Acceptance Criteria

- [ ] The LLM Provider section is visible in the Settings page as a dedicated card.
- [ ] The API key field is password type with show/hide toggle.
- [ ] The Model ID field is a text field with helper text and example.
- [ ] Inline validation: both fields required for save.
- [ ] The "Verify Connection" button performs `GET /api/v1/models` on OpenRouter with the API key.
- [ ] Successful verification: green badge. Failed verification: red badge with error message specific to error type.
- [ ] The API key is encrypted with `IDataProtector` before saving to the database.
- [ ] The API key is decrypted on load to populate the field (obfuscated).
- [ ] On load: "Configured" badge (green) or "Not configured" badge (orange) based on presence of values.
- [ ] Handling of undecryptable API key (protection key changed).
- [ ] Unit tests cover: encryption/decryption, validation, connection verification error handling.

## Dependencies

- SPEC-settings-page (the section lives within the Settings page and uses its persistence infrastructure).

## Open Questions

- None.
