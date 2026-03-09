using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IOpenCodeSessionService"/> that communicates with the
/// OpenCode Server REST API over HTTP for all session management operations. Uses a typed
/// <see cref="HttpClient"/> (via <see cref="IHttpClientFactory"/>) with no fixed base address — the
/// server URL is resolved at request time to support runtime configuration changes.
/// All methods return result objects and never throw exceptions to the caller (except when
/// the caller's own <see cref="CancellationToken"/> is cancelled).
/// </summary>
public sealed class OpenCodeSessionService : IOpenCodeSessionService
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCodeSessionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeSessionService"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client configured by <see cref="IHttpClientFactory"/>. Must have a default
    /// timeout set at DI registration time (typically 30 seconds) but no <c>BaseAddress</c>.
    /// </param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeSessionService(HttpClient httpClient, ILogger<OpenCodeSessionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<SessionDto>> CreateSessionAsync(
        string baseUrl,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<SessionDto>(
            "CreateSession",
            baseUrl,
            "/session",
            async (uri, ct) =>
            {
                var body = new CreateSessionRequest(title);
                using var response = await _httpClient.PostAsJsonAsync(uri, body, CamelCaseOptions, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<SessionDto>("CreateSession", response, baseUrl);
                }

                return await ReadSessionResponseAsync(response, "CreateSession", baseUrl, ct);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SessionDto>>> ListSessionsAsync(
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IReadOnlyList<SessionDto>>(
            "ListSessions",
            baseUrl,
            "/session",
            async (uri, ct) =>
            {
                using var response = await _httpClient.GetAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<IReadOnlyList<SessionDto>>("ListSessions", response, baseUrl);
                }

                var sessions = await response.Content.ReadFromJsonAsync<List<SessionResponse>>(
                    CamelCaseOptions, ct);

                if (sessions is null)
                {
                    _logger.LogWarning(
                        "ListSessions returned null response body from {BaseUrl}", baseUrl);
                    return Result<IReadOnlyList<SessionDto>>.Failure("Unexpected response format");
                }

                var dtos = new List<SessionDto>(sessions.Count);
                foreach (var session in sessions)
                {
                    var mapResult = MapToSessionDto(session);
                    if (mapResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "ListSessions skipped a session with missing ID from {BaseUrl}", baseUrl);
                        continue;
                    }

                    dtos.Add(mapResult.Value);
                }

                _logger.LogInformation(
                    "ListSessions succeeded with {SessionCount} sessions from {BaseUrl}",
                    dtos.Count, baseUrl);

                return Result<IReadOnlyList<SessionDto>>.Success(dtos);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<SessionDto>> GetSessionAsync(
        string baseUrl,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<SessionDto>.Failure("Session ID is required.");
        }

        return await ExecuteAsync<SessionDto>(
            "GetSession",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}",
            async (uri, ct) =>
            {
                using var response = await _httpClient.GetAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<SessionDto>("GetSession", response, baseUrl);
                }

                return await ReadSessionResponseAsync(response, "GetSession", baseUrl, ct);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteSessionAsync(
        string baseUrl,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result.Failure("Session ID is required.");
        }

        return await ExecuteAsync(
            "DeleteSession",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}",
            async (uri, ct) =>
            {
                using var response = await _httpClient.DeleteAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure("DeleteSession", response, baseUrl);
                }

                _logger.LogInformation(
                    "DeleteSession succeeded for session {SessionId} from {BaseUrl}",
                    sessionId, baseUrl);

                return Result.Success();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> AbortSessionAsync(
        string baseUrl,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result.Failure("Session ID is required.");
        }

        return await ExecuteAsync(
            "AbortSession",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/abort",
            async (uri, ct) =>
            {
                using var response = await _httpClient.PostAsync(uri, content: null, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure("AbortSession", response, baseUrl);
                }

                _logger.LogInformation(
                    "AbortSession succeeded for session {SessionId} from {BaseUrl}",
                    sessionId, baseUrl);

                return Result.Success();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<SessionDto>> UpdateSessionAsync(
        string baseUrl,
        string sessionId,
        string title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<SessionDto>.Failure("Session ID is required.");
        }

        return await ExecuteAsync<SessionDto>(
            "UpdateSession",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}",
            async (uri, ct) =>
            {
                var body = new UpdateSessionRequest(title);
                using var request = new HttpRequestMessage(HttpMethod.Patch, uri)
                {
                    Content = JsonContent.Create(body, options: CamelCaseOptions)
                };

                using var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<SessionDto>("UpdateSession", response, baseUrl);
                }

                return await ReadSessionResponseAsync(response, "UpdateSession", baseUrl, ct);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyDictionary<string, SessionStatusDto>>> GetSessionStatusesAsync(
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IReadOnlyDictionary<string, SessionStatusDto>>(
            "GetSessionStatuses",
            baseUrl,
            "/session/status",
            async (uri, ct) =>
            {
                using var response = await _httpClient.GetAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<IReadOnlyDictionary<string, SessionStatusDto>>(
                        "GetSessionStatuses", response, baseUrl);
                }

                var statuses = await response.Content
                    .ReadFromJsonAsync<Dictionary<string, SessionStatusResponse>>(CamelCaseOptions, ct);

                if (statuses is null)
                {
                    _logger.LogWarning(
                        "GetSessionStatuses returned null response body from {BaseUrl}", baseUrl);
                    return Result<IReadOnlyDictionary<string, SessionStatusDto>>.Failure(
                        "Unexpected response format");
                }

                var mapped = new Dictionary<string, SessionStatusDto>(statuses.Count);
                foreach (var (sessionId, statusResponse) in statuses)
                {
                    mapped[sessionId] = new SessionStatusDto(statusResponse.Status ?? "unknown");
                }

                _logger.LogInformation(
                    "GetSessionStatuses succeeded with {StatusCount} entries from {BaseUrl}",
                    mapped.Count, baseUrl);

                return Result<IReadOnlyDictionary<string, SessionStatusDto>>.Success(mapped);
            },
            cancellationToken);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Executes an HTTP operation with standard URI building, error handling, and logging.
    /// </summary>
    private async Task<Result<T>> ExecuteAsync<T>(
        string operationName,
        string baseUrl,
        string path,
        Func<Uri, CancellationToken, Task<Result<T>>> action,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "{OperationName} starting against {BaseUrl}{Path}",
            operationName, baseUrl, path);

        Uri uri;
        try
        {
            uri = new Uri(baseUrl.TrimEnd('/') + path);
        }
        catch (UriFormatException)
        {
            _logger.LogWarning("Malformed OpenCode Server URL: {BaseUrl}", baseUrl);
            return Result<T>.Failure("Invalid server URL format.");
        }

        try
        {
            return await action(uri, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("{OperationName} timed out for {BaseUrl}", operationName, baseUrl);
            return Result<T>.Failure("Request timed out");
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "{OperationName} connection refused for {BaseUrl}", operationName, baseUrl);
            return Result<T>.Failure("Connection refused");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "{OperationName} network error for {BaseUrl}", operationName, baseUrl);
            return Result<T>.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex, "{OperationName} returned unparseable response from {BaseUrl}", operationName, baseUrl);
            return Result<T>.Failure("Unexpected response format");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "An unexpected error occurred during {OperationName} for {BaseUrl}", operationName, baseUrl);
            return Result<T>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes an HTTP operation that returns a non-generic <see cref="Result"/> with standard
    /// URI building, error handling, and logging.
    /// </summary>
    private async Task<Result> ExecuteAsync(
        string operationName,
        string baseUrl,
        string path,
        Func<Uri, CancellationToken, Task<Result>> action,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "{OperationName} starting against {BaseUrl}{Path}",
            operationName, baseUrl, path);

        Uri uri;
        try
        {
            uri = new Uri(baseUrl.TrimEnd('/') + path);
        }
        catch (UriFormatException)
        {
            _logger.LogWarning("Malformed OpenCode Server URL: {BaseUrl}", baseUrl);
            return Result.Failure("Invalid server URL format.");
        }

        try
        {
            return await action(uri, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("{OperationName} timed out for {BaseUrl}", operationName, baseUrl);
            return Result.Failure("Request timed out");
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "{OperationName} connection refused for {BaseUrl}", operationName, baseUrl);
            return Result.Failure("Connection refused");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "{OperationName} network error for {BaseUrl}", operationName, baseUrl);
            return Result.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex, "{OperationName} returned unparseable response from {BaseUrl}", operationName, baseUrl);
            return Result.Failure("Unexpected response format");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "An unexpected error occurred during {OperationName} for {BaseUrl}", operationName, baseUrl);
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads and deserializes a single <see cref="SessionResponse"/> from the HTTP response,
    /// then maps it to a <see cref="SessionDto"/>.
    /// </summary>
    private async Task<Result<SessionDto>> ReadSessionResponseAsync(
        HttpResponseMessage response,
        string operationName,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var sessionResponse = await response.Content.ReadFromJsonAsync<SessionResponse>(
            CamelCaseOptions, cancellationToken);

        if (sessionResponse is null)
        {
            _logger.LogWarning(
                "{OperationName} returned null response body from {BaseUrl}", operationName, baseUrl);
            return Result<SessionDto>.Failure("Unexpected response format");
        }

        var mapResult = MapToSessionDto(sessionResponse);
        if (mapResult.IsFailure)
        {
            _logger.LogWarning(
                "{OperationName} returned a session with missing ID from {BaseUrl}", operationName, baseUrl);
            return mapResult;
        }

        _logger.LogInformation(
            "{OperationName} succeeded for session {SessionId} from {BaseUrl}",
            operationName, mapResult.Value.Id, baseUrl);

        return mapResult;
    }

    /// <summary>
    /// Logs an HTTP non-success status code and returns a typed failure result.
    /// </summary>
    private Result<T> LogAndReturnHttpFailure<T>(
        string operationName,
        HttpResponseMessage response,
        string baseUrl)
    {
        var statusCode = (int)response.StatusCode;
        _logger.LogWarning(
            "{OperationName} returned non-success status {StatusCode} from {BaseUrl}",
            operationName, statusCode, baseUrl);
        return Result<T>.Failure($"Server error ({statusCode})");
    }

    /// <summary>
    /// Logs an HTTP non-success status code and returns a non-generic failure result.
    /// </summary>
    private Result LogAndReturnHttpFailure(
        string operationName,
        HttpResponseMessage response,
        string baseUrl)
    {
        var statusCode = (int)response.StatusCode;
        _logger.LogWarning(
            "{OperationName} returned non-success status {StatusCode} from {BaseUrl}",
            operationName, statusCode, baseUrl);
        return Result.Failure($"Server error ({statusCode})");
    }

    /// <summary>
    /// Converts an internal <see cref="SessionResponse"/> to a public <see cref="SessionDto"/>.
    /// Returns a failure result if the session ID is missing.
    /// </summary>
    private static Result<SessionDto> MapToSessionDto(SessionResponse response)
    {
        if (response.Id is null)
        {
            return Result<SessionDto>.Failure("Session response is missing a required ID field.");
        }

        var createdAt = response.CreatedAt.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(response.CreatedAt.Value)
            : DateTimeOffset.MinValue;

        var updatedAt = response.UpdatedAt.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(response.UpdatedAt.Value)
            : DateTimeOffset.MinValue;

        return Result<SessionDto>.Success(new SessionDto(response.Id, response.Title, createdAt, updatedAt));
    }

    // ── Internal DTOs for JSON deserialization ──────────────────────────────

    /// <summary>
    /// Mirrors the JSON shape returned by the OpenCode Server for a session object.
    /// Timestamps are Unix milliseconds.
    /// </summary>
    private sealed record SessionResponse(string? Id, string? Title, long? CreatedAt, long? UpdatedAt);

    /// <summary>
    /// Mirrors the JSON shape returned by the OpenCode Server for a session status entry.
    /// </summary>
    private sealed record SessionStatusResponse(string? Status);

    /// <summary>
    /// Request body for <c>POST /session</c>.
    /// </summary>
    private sealed record CreateSessionRequest(string? Title);

    /// <summary>
    /// Request body for <c>PATCH /session/:id</c>.
    /// </summary>
    private sealed record UpdateSessionRequest(string? Title);
}
