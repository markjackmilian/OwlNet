# SPEC-004: OpenCode Client Core

> **Status:** Todo
> **Created:** 2026-03-07
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OwlNet needs to interact programmatically with OpenCode Server — a local HTTP server exposed by the `opencode serve` command (OpenAPI 3.1). This spec defines the **foundational client layer**: connection configuration, health checking, shared HTTP client infrastructure, common response/error models, and connection status tracking. All other OpenCode integration specs depend on this one.

In the first phase, the OpenCode Server instance is started externally by the user (not managed by OwlNet). OwlNet connects to it as a client. The server URL defaults to `http://127.0.0.1:4096` but is configurable by the user from the Settings page. Authentication is not required in this phase.

Each OpenCode Server instance operates in the context of a specific project directory. OwlNet's `Project` entity (already existing in the database) is linked to the OpenCode Server: the server works on the same directory as the OwlNet project.

## Actors

- **User** — configures the OpenCode Server connection from the Settings page.
- **Backend services** — DispatchR handlers and other services that use the client to interact with OpenCode Server.
- **OpenCode Server** — external HTTP server running locally, exposing the OpenAPI 3.1 spec.

## Functional Requirements

1. The system SHALL define an `IOpenCodeClient` interface in the Application layer that serves as the entry point for all OpenCode Server interactions.
2. The system SHALL implement `IOpenCodeClient` in the Infrastructure layer using `HttpClient` (typed client pattern via `IHttpClientFactory`).
3. The system SHALL store the OpenCode Server base URL in the database via the existing `IAppSettingService` under the key `OpenCode:ServerUrl`, with a default value of `http://127.0.0.1:4096`.
4. The system SHALL provide a "OpenCode Server" configuration section in the Settings page (as a MudBlazor card) where the user can view and edit the server URL.
5. The system SHALL expose a `HealthCheckAsync` method on `IOpenCodeClient` that calls `GET /global/health` and returns a result containing `IsHealthy` (bool) and `Version` (string).
6. The system SHALL display the connection status in the Settings page OpenCode Server card: a green badge "Connected — v{version}" when healthy, or a red badge "Not connected" with error details when unhealthy.
7. The system SHALL provide a "Test Connection" button in the Settings card that triggers the health check on demand.
8. The system SHALL define common response models (records/DTOs) in the Application layer for shared OpenCode API types: error responses, pagination, and base result wrapper.
9. The system SHALL handle HTTP errors from OpenCode Server gracefully, mapping them to a typed `OpenCodeApiException` or returning failure `Result<T>` values (consistent with the existing Result pattern in the codebase).
10. The system SHALL enforce a configurable HTTP timeout for all OpenCode Server calls (default: 30 seconds for standard calls).
11. The system SHALL log all OpenCode Server interactions using structured logging (`ILogger<T>`) at appropriate levels: Information for successful calls, Warning for retryable failures, Error for unrecoverable failures.
12. The system SHALL resolve the server base URL at request time (not at DI registration time) to support runtime configuration changes without restarting the application.

## User Flow

### Happy Path — Configure and verify connection
1. The user opens the Settings page.
2. The "OpenCode Server" card displays the current URL (default `http://127.0.0.1:4096`) in an editable text field.
3. The user clicks "Test Connection".
4. The system calls `GET /global/health` on the configured URL.
5. The server responds with `{ healthy: true, version: "1.2.20" }`.
6. The card displays a green badge "Connected — v1.2.20".
7. The user clicks "Save" to persist the URL.

### Happy Path — Default configuration works
1. The user opens the Settings page.
2. The OpenCode Server card auto-checks the connection on load.
3. The server is running on the default URL. Green badge displayed.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| OpenCode Server is not running | Red badge "Not connected — Connection refused". No crash. |
| Server URL is malformed (not a valid URL) | Inline validation prevents save. Error message "Invalid URL format". |
| Server URL is empty | Inline validation prevents save. Error message "Server URL is required". |
| Health check times out (>30s) | Red badge "Not connected — Request timed out". |
| Server returns unexpected HTTP status (e.g., 500) | Red badge "Not connected — Server error (500)". |
| Server returns non-JSON response | Red badge "Not connected — Unexpected response format". |
| Network error (DNS resolution, firewall) | Red badge "Not connected — Network error: {details}". |
| URL changed at runtime after save | Next API call uses the new URL without application restart. |

## Out of Scope

- Starting or stopping the OpenCode Server process from OwlNet (future phase).
- HTTP Basic Auth support (future, when `OPENCODE_SERVER_PASSWORD` is needed).
- Retry policies and circuit breaker patterns (can be added later as cross-cutting concern).
- Any specific API domain calls (sessions, messages, files) — those are in separate specs.

## Acceptance Criteria

- [ ] `IOpenCodeClient` interface is defined in the Application layer with `HealthCheckAsync` method.
- [ ] Implementation uses typed `HttpClient` via `IHttpClientFactory` in the Infrastructure layer.
- [ ] Server URL is persisted in the database via `IAppSettingService` with key `OpenCode:ServerUrl`.
- [ ] Default URL is `http://127.0.0.1:4096` when no setting is saved.
- [ ] Settings page displays an "OpenCode Server" card with URL field, "Test Connection" button, and "Save" button.
- [ ] Health check calls `GET /global/health` and displays green/red badge based on result.
- [ ] Auto-check on section load.
- [ ] HTTP errors are mapped to typed results (not unhandled exceptions).
- [ ] Structured logging for all API interactions.
- [ ] URL changes take effect at runtime without restart.
- [ ] Common response DTOs (records) are defined in the Application layer.
- [ ] Unit tests cover: health check success, health check failure (various error types), URL validation, timeout handling.

## Dependencies

- SPEC-settings-page (the card lives within the Settings page).
- SPEC-opencode-health-check (OpenCode CLI must be installed for the server to be available).

## Open Questions

- None.
