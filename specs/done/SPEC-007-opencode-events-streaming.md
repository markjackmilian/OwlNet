# SPEC-007: OpenCode Events Streaming

> **Status:** Done
> **Created:** 2026-03-07
> **Author:** owl-planner + user
> **Priority:** High
> **Estimated Complexity:** M

## Context

OpenCode Server exposes a Server-Sent Events (SSE) endpoint (`GET /event`) that streams real-time events about everything happening on the server: session status changes, message updates, tool executions, errors, and more. The first event is always `server.connected`.

OwlNet needs to subscribe to this event stream to keep the UI and backend state synchronized with the OpenCode Server. In this phase, the focus is on **polling-compatible event consumption** — the service subscribes to the SSE stream and exposes events to consumers (UI components, backend services) through an in-process notification mechanism. Full real-time token-by-token streaming to the Blazor UI is not required; complete response polling is sufficient.

## Actors

- **Backend services** — subscribe to OpenCode events to react to state changes (e.g., session completed, message received).
- **UI components** — consume processed events to update the display (e.g., session status badges, notification toasts).
- **OpenCode Server** — emits SSE events on the `/event` endpoint.

## Functional Requirements

1. The system SHALL define an `IOpenCodeEventService` interface in the Application layer for subscribing to and consuming OpenCode Server events.
2. The system SHALL implement `IOpenCodeEventService` in the Infrastructure layer using `HttpClient` with SSE stream reading.
3. The system SHALL connect to `GET /event` on the OpenCode Server and maintain a persistent SSE connection.
4. The system SHALL parse incoming SSE events into typed `OpenCodeEventDto` records containing at minimum: `Type` (string, e.g., "session.updated", "message.updated"), `Timestamp`, and `Data` (deserialized JSON payload).
5. The system SHALL expose an event notification mechanism (e.g., an observable pattern, event callbacks, or an in-process event bus) that allows multiple consumers to subscribe to specific event types.
6. The system SHALL automatically reconnect to the SSE stream if the connection is lost, with an exponential backoff strategy (initial delay: 1 second, max delay: 30 seconds).
7. The system SHALL emit a local "connection lost" event when the SSE connection drops, and a "connection restored" event when it reconnects.
8. The system SHALL support starting and stopping the event subscription explicitly (the subscription should not run when no consumers need it, or when the server is not configured).
9. The system SHALL handle the initial `server.connected` event as confirmation that the SSE stream is active.
10. The system SHALL log SSE connection lifecycle events (connected, disconnected, reconnecting, reconnected) at Information level, and individual events at Debug level.
11. The system SHALL define well-known event type constants (e.g., `OpenCodeEventTypes.SessionUpdated`, `OpenCodeEventTypes.MessageUpdated`) for type-safe event filtering.
12. The system SHALL properly dispose of the SSE connection and clean up resources when the service is disposed.

## User Flow

### Happy Path — Event stream active
1. OwlNet starts and the OpenCode Server is configured and running.
2. The event service connects to `GET /event`.
3. The server sends `server.connected` as the first event.
4. The service emits a "connected" notification to subscribers.
5. As the user interacts with OpenCode (via SPEC-006 messaging), events flow in: `session.updated`, `message.updated`, etc.
6. UI components subscribed to these events update their display accordingly (e.g., session status badge changes from "running" to "idle").

### Reconnection scenario
1. The OpenCode Server is restarted.
2. The SSE connection drops.
3. The event service emits a "connection lost" notification.
4. The service waits 1 second, then attempts to reconnect.
5. If the server is back, the connection is restored. "Connection restored" notification emitted.
6. If not, the service retries with exponential backoff (2s, 4s, 8s, ..., max 30s).

## Edge Cases and Error Handling

| Scenario | Expected Behavior |
|----------|-------------------|
| OpenCode Server is not running when event service starts | Log warning. Enter reconnection loop with backoff. |
| SSE stream returns malformed event data | Log warning with raw data. Skip the event. Do not crash. |
| SSE stream returns unknown event type | Wrap in generic `OpenCodeEventDto` with raw data. Log at Debug level. |
| Server URL changes at runtime | Stop current subscription, reconnect to new URL. |
| Multiple consumers subscribe to the same event type | All consumers receive the event (multicast). |
| Consumer throws exception during event handling | Log error. Do not propagate to other consumers or crash the stream. |
| Application shutdown | Gracefully close the SSE connection and dispose resources. |
| Network intermittent (frequent disconnects) | Backoff prevents aggressive reconnection. Log pattern at Warning level. |

## Out of Scope

- Token-by-token streaming of AI responses to the Blazor UI (SignalR push). Polling complete responses is sufficient for this phase.
- Filtering events server-side (the server sends all events; filtering happens client-side).
- Persisting events to the database.
- Global event stream (`GET /global/event`) — only the main `/event` endpoint is covered.
- UI components for displaying events (will be part of separate UI specs).

## Acceptance Criteria

- [x] `IOpenCodeEventService` interface is defined in the Application layer.
- [x] Implementation connects to `GET /event` SSE endpoint in the Infrastructure layer.
- [x] SSE events are parsed into typed `OpenCodeEventDto` records.
- [x] Multiple consumers can subscribe to events by type.
- [x] Automatic reconnection with exponential backoff (1s initial, 30s max).
- [x] "Connection lost" and "connection restored" notifications are emitted.
- [x] `server.connected` event is handled as stream activation confirmation.
- [x] Well-known event type constants are defined.
- [x] Subscription can be started and stopped explicitly.
- [x] Graceful disposal of SSE connection on shutdown.
- [x] Structured logging for connection lifecycle and events.
- [x] Malformed or unknown events are handled gracefully (no crashes).
- [x] Unit tests cover: event parsing, reconnection logic, consumer notification, error handling, disposal.

## Dependencies

- SPEC-004 (provides HTTP client infrastructure and connection configuration).

## Open Questions

- None.
