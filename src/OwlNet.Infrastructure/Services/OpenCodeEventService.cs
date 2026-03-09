using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Constants;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Singleton SSE client that connects to the OpenCode Server's <c>GET /event</c> endpoint,
/// parses incoming Server-Sent Events into <see cref="OpenCodeEventDto"/> records, and
/// distributes them to registered subscribers using a multicast event bus.
/// <para>
/// Handles automatic reconnection with exponential backoff (1 s initial, 30 s max) when the
/// connection is lost, and emits synthetic <c>connection.lost</c> / <c>connection.restored</c>
/// lifecycle events so subscribers can react to connectivity changes.
/// </para>
/// <para>
/// Uses <see cref="IServiceScopeFactory"/> to resolve scoped services (e.g.,
/// <see cref="IAppSettingService"/>) per-connection, following the same pattern as
/// <see cref="OpenCodeServerManager"/> to avoid captive dependency issues.
/// </para>
/// </summary>
public sealed class OpenCodeEventService : IOpenCodeEventService
{
    /// <summary>
    /// Initial reconnection delay in seconds before exponential backoff increases it.
    /// </summary>
    private const int InitialReconnectDelaySeconds = 1;

    /// <summary>
    /// Maximum reconnection delay in seconds — the backoff will not exceed this value.
    /// </summary>
    private const int MaxReconnectDelaySeconds = 30;

    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOpenCodeServerManager _serverManager;
    private readonly ILogger<OpenCodeEventService> _logger;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<OpenCodeEventDto, Task>>> _subscribers = new();

    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;
    private volatile bool _isConnected;
    private volatile bool _hasEverConnected;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeEventService"/> class and subscribes
    /// to server manager status changes for automatic lifecycle coordination.
    /// </summary>
    /// <param name="httpClient">A typed <see cref="HttpClient"/> configured with infinite timeout for long-lived SSE connections.</param>
    /// <param name="scopeFactory">The scope factory used to resolve scoped services such as <see cref="IAppSettingService"/> per-connection.</param>
    /// <param name="serverManager">The singleton server manager whose status changes trigger automatic start/stop of the SSE connection.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeEventService(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IOpenCodeServerManager serverManager,
        ILogger<OpenCodeEventService> logger)
    {
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
        _serverManager = serverManager;
        _logger = logger;

        _serverManager.OnStatusChanged += OnServerStatusChanged;
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public IDisposable Subscribe(string eventType, Func<OpenCodeEventDto, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(handler);

        var bag = _subscribers.GetOrAdd(eventType, _ => []);
        bag.Add(handler);

        return new Subscription(this, eventType, handler);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (_backgroundTask is not null && !_backgroundTask.IsCompleted)
            {
                _logger.LogDebug("SSE event service is already running");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _backgroundTask = Task.Run(() => RunSseLoopAsync(token), CancellationToken.None);
            _logger.LogInformation("SSE event service started");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            await StopInternalAsync();
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _serverManager.OnStatusChanged -= OnServerStatusChanged;

        // Best-effort synchronous stop
        try
        {
            _cts?.Cancel();
            _backgroundTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Best effort — background task may have already completed or faulted.
        }

        _cts?.Dispose();
        _lifecycleLock.Dispose();
    }

    // ── Server manager integration ──────────────────────────────────────

    /// <summary>
    /// Handles <see cref="IOpenCodeServerManager.OnStatusChanged"/> events to automatically
    /// start or stop the SSE connection based on server state transitions.
    /// Uses <c>async void</c> because this is an event handler — all exceptions are caught
    /// and logged to prevent unobserved task exceptions.
    /// </summary>
    private async void OnServerStatusChanged()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var state = _serverManager.CurrentStatus.State;

            switch (state)
            {
                case OpenCodeServerState.Running:
                    await StartAsync();
                    break;
                case OpenCodeServerState.Stopped:
                case OpenCodeServerState.Error:
                case OpenCodeServerState.Stopping:
                    await StopAsync();
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            // Race between status change and disposal — safe to ignore.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling server status change in SSE event service");
        }
    }

    // ── SSE connection loop ─────────────────────────────────────────────

    /// <summary>
    /// Main background loop that maintains the SSE connection, reads events, and handles
    /// reconnection with exponential backoff when the connection is lost.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the service should stop.</param>
    private async Task RunSseLoopAsync(CancellationToken cancellationToken)
    {
        var reconnectDelay = TimeSpan.FromSeconds(InitialReconnectDelaySeconds);
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var serverUrl = await ResolveServerUrlAsync(cancellationToken);
                await ReadSseStreamAsync(serverUrl, cancellationToken);

                // Stream ended gracefully — reset backoff so the next reconnection starts fresh.
                reconnectDelay = TimeSpan.FromSeconds(InitialReconnectDelaySeconds);
                attempt = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SSE connection error occurred");
            }

            // Connection was lost or failed — emit synthetic event if we were connected
            if (_isConnected)
            {
                _isConnected = false;
                await EmitSyntheticEventAsync(OpenCodeEventTypes.ConnectionLost);
                _logger.LogInformation("SSE connection lost");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Reconnection with exponential backoff
            attempt++;
            _logger.LogInformation(
                "Reconnecting to SSE stream (attempt {Attempt}, delay {DelaySeconds}s)",
                attempt, reconnectDelay.TotalSeconds);

            try
            {
                await Task.Delay(reconnectDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            reconnectDelay = CalculateNextDelay(reconnectDelay);
        }

        _isConnected = false;
        _logger.LogInformation("SSE event service stopped");
    }

    /// <summary>
    /// Connects to the SSE endpoint and reads events until the stream ends or an error occurs.
    /// When a <c>server.connected</c> event is received after a previous connection loss, a
    /// <c>connection.restored</c> synthetic event is emitted to subscribers.
    /// </summary>
    /// <param name="serverUrl">The base URL of the OpenCode Server.</param>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    private async Task ReadSseStreamAsync(string serverUrl, CancellationToken cancellationToken)
    {
        var eventUrl = serverUrl.TrimEnd('/') + "/event";

        using var request = new HttpRequestMessage(HttpMethod.Get, eventUrl);
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("SSE connection established to {ServerUrl}", serverUrl);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEventType = null;
        string? currentData = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                // Stream ended (server closed the connection)
                break;
            }

            ProcessSseLine(line, ref currentEventType, ref currentData);

            // Empty line signals end of an event
            if (line.Length == 0 && currentEventType is not null)
            {
                var dto = CreateEventDto(currentEventType, currentData);
                await DistributeEventAsync(dto);

                currentEventType = null;
                currentData = null;
            }
        }
    }

    /// <summary>
    /// Processes a single line from the SSE stream, accumulating the event type and data fields.
    /// </summary>
    /// <param name="line">The raw line read from the SSE stream.</param>
    /// <param name="currentEventType">The accumulated event type, updated if the line is an <c>event:</c> field.</param>
    /// <param name="currentData">The accumulated data payload, updated if the line is a <c>data:</c> field.</param>
    private void ProcessSseLine(string line, ref string? currentEventType, ref string? currentData)
    {
        if (line.StartsWith("event:", StringComparison.Ordinal))
        {
            currentEventType = line["event:".Length..].Trim();
        }
        else if (line.StartsWith("data:", StringComparison.Ordinal))
        {
            var value = line["data:".Length..].Trim();
            currentData = currentData is null ? value : currentData + "\n" + value;
        }
        else if (line.StartsWith(':'))
        {
            // SSE comment (often used as keep-alive) — ignore
        }
    }

    /// <summary>
    /// Creates an <see cref="OpenCodeEventDto"/> from the parsed SSE event type and raw data string.
    /// Attempts to parse the data as JSON; if parsing fails, sets <see cref="OpenCodeEventDto.Data"/>
    /// to <c>null</c> and logs a warning.
    /// </summary>
    /// <param name="eventType">The SSE event type string.</param>
    /// <param name="rawData">The raw data payload string, or <c>null</c> if no data field was present.</param>
    /// <returns>A fully constructed <see cref="OpenCodeEventDto"/>.</returns>
    private OpenCodeEventDto CreateEventDto(string eventType, string? rawData)
    {
        JsonElement? data = null;

        if (rawData is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawData);
                data = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to parse SSE event data: {RawData}", rawData);
            }
        }

        return new OpenCodeEventDto(eventType, DateTimeOffset.UtcNow, data);
    }

    // ── Event distribution ──────────────────────────────────────────────

    /// <summary>
    /// Distributes a parsed event to all matching subscribers: those registered for the specific
    /// event type and those registered for <see cref="IOpenCodeEventService.AllEvents"/>.
    /// Handles the <c>server.connected</c> event as a special case to update connection state.
    /// </summary>
    /// <param name="dto">The event to distribute.</param>
    private async Task DistributeEventAsync(OpenCodeEventDto dto)
    {
        await HandleConnectionStateEventAsync(dto);

        _logger.LogDebug("SSE event received: {EventType}", dto.Type);

        // Notify type-specific subscribers
        if (_subscribers.TryGetValue(dto.Type, out var typeHandlers))
        {
            await InvokeHandlersAsync(dto, typeHandlers);
        }

        // Notify wildcard subscribers
        if (_subscribers.TryGetValue(IOpenCodeEventService.AllEvents, out var wildcardHandlers))
        {
            await InvokeHandlersAsync(dto, wildcardHandlers);
        }
    }

    /// <summary>
    /// Updates internal connection state based on well-known event types. When a
    /// <c>server.connected</c> event is received after a previous connection (reconnection),
    /// emits a <c>connection.restored</c> synthetic event to subscribers.
    /// </summary>
    /// <param name="dto">The event to inspect.</param>
    /// <returns>A task that completes when any synthetic events have been distributed.</returns>
    private async Task HandleConnectionStateEventAsync(OpenCodeEventDto dto)
    {
        if (dto.Type != OpenCodeEventTypes.ServerConnected)
        {
            return;
        }

        var isReconnection = _hasEverConnected;
        _isConnected = true;
        _hasEverConnected = true;
        _logger.LogInformation("SSE stream confirmed active via server.connected event");

        if (isReconnection)
        {
            _logger.LogInformation("SSE connection restored");
            await EmitSyntheticEventAsync(OpenCodeEventTypes.ConnectionRestored);
        }
    }

    /// <summary>
    /// Invokes all handlers in a bag, catching and logging exceptions from individual handlers
    /// so that one failing consumer does not affect others or crash the event stream.
    /// </summary>
    /// <param name="dto">The event to pass to each handler.</param>
    /// <param name="handlers">The collection of handler callbacks to invoke.</param>
    private async Task InvokeHandlersAsync(OpenCodeEventDto dto, ConcurrentBag<Func<OpenCodeEventDto, Task>> handlers)
    {
        foreach (var handler in handlers)
        {
            try
            {
                await handler(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Consumer threw exception handling {EventType} event",
                    dto.Type);
            }
        }
    }

    /// <summary>
    /// Emits a synthetic lifecycle event (e.g., <c>connection.lost</c>, <c>connection.restored</c>)
    /// to all subscribers without any data payload.
    /// </summary>
    /// <param name="eventType">The synthetic event type to emit.</param>
    private async Task EmitSyntheticEventAsync(string eventType)
    {
        var dto = new OpenCodeEventDto(eventType, DateTimeOffset.UtcNow, null);
        await DistributeEventAsync(dto);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the OpenCode Server URL from application settings using a scoped
    /// <see cref="IAppSettingService"/>. Falls back to <see cref="OpenCodeConstants.DefaultServerUrl"/>
    /// if the setting is not configured.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The resolved server base URL.</returns>
    private async Task<string> ResolveServerUrlAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var appSettingService = scope.ServiceProvider.GetRequiredService<IAppSettingService>();

        var result = await appSettingService.GetByKeyAsync(
            OpenCodeConstants.ServerUrlSettingKey, cancellationToken);

        return result.IsSuccess ? result.Value : OpenCodeConstants.DefaultServerUrl;
    }

    /// <summary>
    /// Calculates the next reconnection delay using exponential backoff, capped at
    /// <see cref="MaxReconnectDelaySeconds"/>.
    /// </summary>
    /// <param name="currentDelay">The current delay before this calculation.</param>
    /// <returns>The next delay, doubled from the current but not exceeding the maximum.</returns>
    private static TimeSpan CalculateNextDelay(TimeSpan currentDelay)
    {
        var nextSeconds = Math.Min(
            currentDelay.TotalSeconds * 2,
            MaxReconnectDelaySeconds);

        return TimeSpan.FromSeconds(nextSeconds);
    }

    /// <summary>
    /// Stops the background SSE loop and cleans up the cancellation token source.
    /// Must be called while holding <see cref="_lifecycleLock"/>.
    /// </summary>
    private async Task StopInternalAsync()
    {
        if (_backgroundTask is null || _backgroundTask.IsCompleted)
        {
            _logger.LogDebug("SSE event service is not running");
            return;
        }

        _logger.LogInformation("Stopping SSE event service");

        try
        {
            _cts?.Cancel();
            await _backgroundTask.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("SSE background task did not stop within the timeout period");
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping SSE background task");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _isConnected = false;
            _hasEverConnected = false;
        }
    }

    /// <summary>
    /// Removes a handler from the subscriber bag for the specified event type.
    /// Called by <see cref="Subscription.Dispose"/> when a consumer unsubscribes.
    /// </summary>
    /// <param name="eventType">The event type the handler was registered for.</param>
    /// <param name="handler">The handler callback to remove.</param>
    private void RemoveHandler(string eventType, Func<OpenCodeEventDto, Task> handler)
    {
        if (!_subscribers.TryGetValue(eventType, out var bag))
        {
            return;
        }

        // ConcurrentBag does not support removal, so we rebuild the bag without the handler.
        // This is acceptable because unsubscribe is an infrequent operation.
        var remaining = new ConcurrentBag<Func<OpenCodeEventDto, Task>>(
            bag.Where(h => h != handler));

        _subscribers.TryUpdate(eventType, remaining, bag);
    }

    // ── Subscription (inner class) ──────────────────────────────────────

    /// <summary>
    /// Represents an active event subscription. Disposing this object removes the handler
    /// from the event service's subscriber registry.
    /// </summary>
    private sealed class Subscription : IDisposable
    {
        private readonly OpenCodeEventService _service;
        private readonly string _eventType;
        private readonly Func<OpenCodeEventDto, Task> _handler;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="service">The owning event service.</param>
        /// <param name="eventType">The event type this subscription is registered for.</param>
        /// <param name="handler">The handler callback to remove on dispose.</param>
        public Subscription(
            OpenCodeEventService service,
            string eventType,
            Func<OpenCodeEventDto, Task> handler)
        {
            _service = service;
            _eventType = eventType;
            _handler = handler;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _service.RemoveHandler(_eventType, _handler);
        }
    }
}
