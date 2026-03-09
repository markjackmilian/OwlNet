namespace OwlNet.Application.Common.Constants;

/// <summary>
/// Well-known event type constants for OpenCode Server SSE events and internal connection lifecycle events.
/// Used for type-safe event filtering when subscribing to the event stream.
/// </summary>
public static class OpenCodeEventTypes
{
    // ── Server events (emitted by OpenCode Server via SSE) ──────────────────

    /// <summary>
    /// First event sent when the SSE stream is established with the OpenCode Server.
    /// </summary>
    public const string ServerConnected = "server.connected";

    /// <summary>
    /// Session state changed on the server (e.g., configuration or metadata update).
    /// </summary>
    public const string SessionUpdated = "session.updated";

    /// <summary>
    /// Session was deleted on the server.
    /// </summary>
    public const string SessionDeleted = "session.deleted";

    /// <summary>
    /// A new message was created in the active session.
    /// </summary>
    public const string MessageCreated = "message.created";

    /// <summary>
    /// An existing message's content was updated (e.g., streaming token append).
    /// </summary>
    public const string MessageUpdated = "message.updated";

    /// <summary>
    /// Message generation completed; the message is now in its final state.
    /// </summary>
    public const string MessageCompleted = "message.completed";

    // ── Local/synthetic events (emitted by the event service) ───────────────

    /// <summary>
    /// The SSE connection to the OpenCode Server was lost unexpectedly.
    /// </summary>
    public const string ConnectionLost = "connection.lost";

    /// <summary>
    /// The SSE connection to the OpenCode Server was re-established after a drop.
    /// </summary>
    public const string ConnectionRestored = "connection.restored";
}
