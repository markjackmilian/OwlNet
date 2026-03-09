using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Service for subscribing to and consuming real-time Server-Sent Events (SSE) from the
/// OpenCode Server's <c>GET /event</c> endpoint. The implementation maintains a persistent
/// SSE connection, parses incoming events into <see cref="OpenCodeEventDto"/> records, and
/// distributes them to registered subscribers using a multicast pattern.
/// <para>
/// Multiple consumers can subscribe to the same event type. Subscribers receive events
/// asynchronously via callbacks. The service handles automatic reconnection with exponential
/// backoff when the connection is lost, and emits synthetic lifecycle events
/// (<c>connection.lost</c>, <c>connection.restored</c>) to notify subscribers of connectivity changes.
/// </para>
/// <para>
/// Registered as a singleton. Implements <see cref="IDisposable"/> to ensure the SSE connection
/// and all internal resources are cleaned up on application shutdown.
/// </para>
/// </summary>
public interface IOpenCodeEventService : IDisposable
{
    /// <summary>
    /// Wildcard event type that matches all events. Pass this value to
    /// <see cref="Subscribe"/> to receive every event regardless of its type.
    /// </summary>
    const string AllEvents = "*";

    /// <summary>
    /// Gets a value indicating whether the SSE connection to the OpenCode Server is currently
    /// active and receiving events. Returns <c>true</c> after a successful connection is
    /// established (i.e., the <c>server.connected</c> event has been received) and <c>false</c>
    /// when the connection is lost or has not yet been started.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Registers a callback to be invoked when an event of the specified type is received from
    /// the OpenCode Server SSE stream. Multiple consumers can subscribe to the same event type
    /// (multicast). The returned <see cref="IDisposable"/> removes the subscription when disposed,
    /// so callers should store and dispose it when they no longer need to receive events.
    /// <para>
    /// Pass <see cref="AllEvents"/> (<c>"*"</c>) as the <paramref name="eventType"/> to receive
    /// all events regardless of type.
    /// </para>
    /// <example>
    /// <code>
    /// // Subscribe to a specific event type:
    /// var subscription = eventService.Subscribe(
    ///     OpenCodeEventTypes.MessageCreated,
    ///     async evt => await ProcessMessageAsync(evt));
    ///
    /// // Subscribe to all events:
    /// var allSub = eventService.Subscribe(
    ///     IOpenCodeEventService.AllEvents,
    ///     async evt => await LogEventAsync(evt));
    ///
    /// // Unsubscribe by disposing:
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="eventType">
    /// The SSE event type to subscribe to (e.g., <c>"session.updated"</c>, <c>"message.created"</c>).
    /// Use constants from <see cref="Constants.OpenCodeEventTypes"/> for type safety, or
    /// <see cref="AllEvents"/> to receive all events.
    /// </param>
    /// <param name="handler">
    /// An asynchronous callback invoked each time a matching event is received. The handler
    /// receives the parsed <see cref="OpenCodeEventDto"/>. Exceptions thrown by the handler
    /// are logged but do not propagate to other subscribers or interrupt the event stream.
    /// </param>
    /// <returns>
    /// An <see cref="IDisposable"/> that, when disposed, removes this subscription. Callers are
    /// responsible for disposing the subscription when they no longer need to receive events.
    /// </returns>
    IDisposable Subscribe(string eventType, Func<OpenCodeEventDto, Task> handler);

    /// <summary>
    /// Starts the SSE connection to the OpenCode Server's <c>GET /event</c> endpoint and begins
    /// receiving events. If the service is already connected, this method returns immediately
    /// without taking any action.
    /// <para>
    /// On a successful connection, the server sends a <c>server.connected</c> event as the first
    /// message, which the service uses as confirmation that the stream is active. If the server
    /// is not reachable, the service enters a reconnection loop with exponential backoff
    /// (initial delay: 1 second, maximum delay: 30 seconds).
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the start operation.</param>
    /// <returns>A task that completes when the connection attempt has been initiated.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the SSE connection to the OpenCode Server and cleans up associated resources.
    /// If the service is not currently connected, this method returns immediately without
    /// taking any action. Active subscribers are not removed — they will resume receiving
    /// events if <see cref="StartAsync"/> is called again.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the stop operation.</param>
    /// <returns>A task that completes when the connection has been closed.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
