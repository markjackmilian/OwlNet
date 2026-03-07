# SPEC-005: OpenCode Session Management

> **Status:** Todo
> **Created:** 2026-03-07
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OpenCode Server organizes AI interactions into **sessions** — each session is a conversation thread with the AI agent. OwlNet needs to manage these sessions programmatically: create new sessions, list existing ones, retrieve session details, abort running sessions, and delete sessions. Sessions are scoped to the current OpenCode project, which is linked to an OwlNet `Project` entity.

This spec covers the service layer for session CRUD operations. It does NOT cover sending messages within a session (see SPEC-006) or real-time event streaming (see SPEC-007).

## Actors

- **User** — interacts with sessions through the OwlNet Blazor UI.
- **Backend services** — DispatchR handlers that orchestrate session operations as part of automated workflows.
- **OpenCode Server** — provides the session REST API.

## Functional Requirements

1. The system SHALL extend `IOpenCodeClient` (or define a dedicated `IOpenCodeSessionService` interface in the Application layer) with methods for session management.
2. The system SHALL support **creating a new session** by calling `POST /session` with an optional `title` parameter, returning a `SessionDto` record.
3. The system SHALL support **listing all sessions** by calling `GET /session`, returning a collection of `SessionDto` records.
4. The system SHALL support **getting session details** by calling `GET /session/:id`, returning a `SessionDto` record with full details.
5. The system SHALL support **deleting a session** by calling `DELETE /session/:id`, returning a success/failure result.
6. The system SHALL support **aborting a running session** by calling `POST /session/:id/abort`, returning a success/failure result.
7. The system SHALL support **updating session properties** (title) by calling `PATCH /session/:id`, returning the updated `SessionDto`.
8. The system SHALL define a `SessionDto` record in the Application layer that maps the relevant fields from the OpenCode Server `Session` type (at minimum: `Id`, `Title`, `CreatedAt`, `UpdatedAt`).
9. The system SHALL define a `SessionStatusDto` record to represent the session's execution status (idle, running, etc.) as returned by `GET /session/status`.
10. The system SHALL support **getting status for all sessions** by calling `GET /session/status`, returning a dictionary mapping session IDs to their status.
11. All session operations SHALL propagate `CancellationToken` and return `Result<T>` or `Result` (consistent with the existing Result pattern).
12. All session operations SHALL log structured information about the operation (session ID, operation type, success/failure).

## User Flow

### Happy Path — Create and manage a session
1. The user navigates to the project workspace in OwlNet.
2. The user initiates a new AI session (e.g., clicks "New Session").
3. The system calls `POST /session` with a title. A new session is created.
4. The session appears in the session list.
5. The user can view session details, rename it, or delete it.

### Happy Path — List and resume sessions
1. The user opens the project workspace.
2. The system calls `GET /session` to load all sessions.
3. The user sees a list of past sessions with titles and timestamps.
4. The user selects a session to view its details or continue the conversation.

### Happy Path — Abort a running session
1. The user sees a session is currently running (status indicator).
2. The user clicks "Abort".
3. The system calls `POST /session/:id/abort`.
4. The session status updates to idle.

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| OpenCode Server is not running | Return failure Result with descriptive error. UI shows connection error. |
| Create session fails (server error) | Return failure Result. UI shows error snackbar. |
| Delete a session that does not exist | Return failure Result. UI shows "Session not found" error. |
| Abort a session that is not running | Return success (idempotent). No error. |
| List sessions when none exist | Return empty collection. UI shows empty state message. |
| Session ID is null or empty | Validate at the service level before making the HTTP call. Return failure Result. |
| Server returns unexpected session format | Log warning, attempt to deserialize what is available, return failure Result if critical fields are missing. |

## Out of Scope

- Sending messages within a session (see SPEC-006).
- Forking or sharing sessions.
- Session summarization.
- Session diff/revert operations.
- UI components for session management (will be a separate UI spec).
- Real-time session status updates via SSE (see SPEC-007).

## Acceptance Criteria

- [ ] Session management methods are defined in an interface in the Application layer.
- [ ] Implementation calls the correct OpenCode Server endpoints in the Infrastructure layer.
- [ ] `SessionDto` and `SessionStatusDto` records are defined in the Application layer.
- [ ] Create session accepts an optional title and returns `Result<SessionDto>`.
- [ ] List sessions returns `Result<IReadOnlyList<SessionDto>>`.
- [ ] Get session by ID returns `Result<SessionDto>`.
- [ ] Delete session returns `Result`.
- [ ] Abort session returns `Result`.
- [ ] Update session (title) returns `Result<SessionDto>`.
- [ ] Get all session statuses returns `Result<IReadOnlyDictionary<string, SessionStatusDto>>`.
- [ ] All methods propagate `CancellationToken`.
- [ ] All methods handle HTTP errors and return typed Results (no unhandled exceptions).
- [ ] Structured logging for all operations.
- [ ] Unit tests cover: create, list, get, delete, abort, update, error scenarios.

## Dependencies

- SPEC-004 (provides the HTTP client infrastructure and connection configuration).

## Open Questions

- None.
