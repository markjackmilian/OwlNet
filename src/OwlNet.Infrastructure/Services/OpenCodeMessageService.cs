using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IOpenCodeMessageService"/> that communicates with the
/// OpenCode Server REST API over HTTP for all messaging operations. Uses a typed
/// <see cref="HttpClient"/> (via <see cref="IHttpClientFactory"/>) with no fixed base address — the
/// server URL is resolved at request time to support runtime configuration changes.
/// All methods return result objects and never throw exceptions to the caller (except when
/// the caller's own <see cref="CancellationToken"/> is cancelled).
/// </summary>
public sealed class OpenCodeMessageService : IOpenCodeMessageService
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCodeMessageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeMessageService"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client configured by <see cref="IHttpClientFactory"/>. Must have a default
    /// timeout set at DI registration time (typically 300 seconds for messaging operations)
    /// but no <c>BaseAddress</c>.
    /// </param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeMessageService(HttpClient httpClient, ILogger<OpenCodeMessageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<MessageWithPartsDto>> SendPromptAsync(
        string baseUrl,
        string sessionId,
        SendPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<MessageWithPartsDto>.Failure("Session ID is required.");
        }

        if (request is null)
        {
            return Result<MessageWithPartsDto>.Failure("Prompt request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<MessageWithPartsDto>.Failure("Prompt text is required.");
        }

        return await ExecuteAsync<MessageWithPartsDto>(
            "SendPrompt",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/message",
            async (uri, ct) =>
            {
                _logger.LogInformation(
                    "SendPrompt sending {PromptLength}-char prompt to session {SessionId}",
                    request.Text.Length, sessionId);

                var body = BuildSendMessageRequest(request);
                var stopwatch = Stopwatch.StartNew();

                using var response = await _httpClient.PostAsJsonAsync(uri, body, CamelCaseOptions, ct);
                stopwatch.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<MessageWithPartsDto>("SendPrompt", response, baseUrl);
                }

                var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(
                    CamelCaseOptions, ct);

                if (messageResponse is null)
                {
                    _logger.LogWarning(
                        "SendPrompt returned null response body from {BaseUrl}", baseUrl);
                    return Result<MessageWithPartsDto>.Failure("Unexpected response format");
                }

                var mapResult = MapToMessageWithPartsDto(messageResponse);
                if (mapResult.IsFailure)
                {
                    _logger.LogWarning(
                        "SendPrompt returned a message with missing ID from {BaseUrl}", baseUrl);
                    return mapResult;
                }

                _logger.LogInformation(
                    "SendPrompt succeeded for session {SessionId} with message {MessageId} in {ElapsedMs}ms",
                    sessionId, mapResult.Value.Message.Id, stopwatch.ElapsedMilliseconds);

                return mapResult;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> SubmitPromptAsync(
        string baseUrl,
        string sessionId,
        SendPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result.Failure("Session ID is required.");
        }

        if (request is null)
        {
            return Result.Failure("Prompt request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result.Failure("Prompt text is required.");
        }

        return await ExecuteAsync(
            "SubmitPrompt",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/prompt_async",
            async (uri, ct) =>
            {
                _logger.LogInformation(
                    "SubmitPrompt sending {PromptLength}-char prompt to session {SessionId}",
                    request.Text.Length, sessionId);

                var body = BuildSendMessageRequest(request);

                using var response = await _httpClient.PostAsJsonAsync(uri, body, CamelCaseOptions, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure("SubmitPrompt", response, baseUrl);
                }

                _logger.LogInformation(
                    "SubmitPrompt succeeded for session {SessionId} from {BaseUrl}",
                    sessionId, baseUrl);

                return Result.Success();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MessageWithPartsDto>>> ListMessagesAsync(
        string baseUrl,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<IReadOnlyList<MessageWithPartsDto>>.Failure("Session ID is required.");
        }

        return await ExecuteAsync<IReadOnlyList<MessageWithPartsDto>>(
            "ListMessages",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/message",
            async (uri, ct) =>
            {
                using var response = await _httpClient.GetAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<IReadOnlyList<MessageWithPartsDto>>(
                        "ListMessages", response, baseUrl);
                }

                var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(
                    CamelCaseOptions, ct);

                if (messages is null)
                {
                    _logger.LogWarning(
                        "ListMessages returned null response body from {BaseUrl}", baseUrl);
                    return Result<IReadOnlyList<MessageWithPartsDto>>.Failure("Unexpected response format");
                }

                var dtos = new List<MessageWithPartsDto>(messages.Count);
                foreach (var message in messages)
                {
                    var mapResult = MapToMessageWithPartsDto(message);
                    if (mapResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "ListMessages skipped a message with missing ID from {BaseUrl}", baseUrl);
                        continue;
                    }

                    dtos.Add(mapResult.Value);
                }

                _logger.LogInformation(
                    "ListMessages succeeded with {MessageCount} messages for session {SessionId} from {BaseUrl}",
                    dtos.Count, sessionId, baseUrl);

                return Result<IReadOnlyList<MessageWithPartsDto>>.Success(dtos);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<MessageWithPartsDto>> GetMessageAsync(
        string baseUrl,
        string sessionId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<MessageWithPartsDto>.Failure("Session ID is required.");
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            return Result<MessageWithPartsDto>.Failure("Message ID is required.");
        }

        return await ExecuteAsync<MessageWithPartsDto>(
            "GetMessage",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/message/{Uri.EscapeDataString(messageId)}",
            async (uri, ct) =>
            {
                using var response = await _httpClient.GetAsync(uri, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure<MessageWithPartsDto>("GetMessage", response, baseUrl);
                }

                var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(
                    CamelCaseOptions, ct);

                if (messageResponse is null)
                {
                    _logger.LogWarning(
                        "GetMessage returned null response body from {BaseUrl}", baseUrl);
                    return Result<MessageWithPartsDto>.Failure("Unexpected response format");
                }

                var mapResult = MapToMessageWithPartsDto(messageResponse);
                if (mapResult.IsFailure)
                {
                    _logger.LogWarning(
                        "GetMessage returned a message with missing ID from {BaseUrl}", baseUrl);
                    return mapResult;
                }

                _logger.LogInformation(
                    "GetMessage succeeded for message {MessageId} in session {SessionId} from {BaseUrl}",
                    mapResult.Value.Message.Id, sessionId, baseUrl);

                return mapResult;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteCommandAsync(
        string baseUrl,
        string sessionId,
        ExecuteCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result.Failure("Session ID is required.");
        }

        if (request is null)
        {
            return Result.Failure("Command request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return Result.Failure("Command name is required.");
        }

        return await ExecuteAsync(
            "ExecuteCommand",
            baseUrl,
            $"/session/{Uri.EscapeDataString(sessionId)}/command",
            async (uri, ct) =>
            {
                _logger.LogInformation(
                    "ExecuteCommand sending command {CommandName} to session {SessionId}",
                    request.Command, sessionId);

                var args = request.Args is { Count: > 0 } ? request.Args : null;
                var body = new CommandRequest(request.Command, args);

                using var response = await _httpClient.PostAsJsonAsync(uri, body, CamelCaseOptions, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return LogAndReturnHttpFailure("ExecuteCommand", response, baseUrl);
                }

                _logger.LogInformation(
                    "ExecuteCommand succeeded for command {CommandName} in session {SessionId} from {BaseUrl}",
                    request.Command, sessionId, baseUrl);

                return Result.Success();
            },
            cancellationToken);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Builds the <see cref="SendMessageRequest"/> body from a <see cref="SendPromptRequest"/>,
    /// including optional model override and agent selection.
    /// </summary>
    private static SendMessageRequest BuildSendMessageRequest(SendPromptRequest request)
    {
        var parts = new List<SendMessagePart>
        {
            new("text", request.Text)
        };

        ModelOverride? model = !string.IsNullOrWhiteSpace(request.ProviderID)
            && !string.IsNullOrWhiteSpace(request.ModelID)
                ? new ModelOverride(request.ProviderID, request.ModelID)
                : null;

        var agent = !string.IsNullOrWhiteSpace(request.Agent) ? request.Agent : null;

        return new SendMessageRequest(parts, model, agent);
    }

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
    /// Converts an internal <see cref="MessageResponse"/> to a public <see cref="MessageWithPartsDto"/>.
    /// Returns a failure result if the message ID is missing.
    /// </summary>
    private static Result<MessageWithPartsDto> MapToMessageWithPartsDto(MessageResponse response)
    {
        if (response.Id is null)
        {
            return Result<MessageWithPartsDto>.Failure("Message response is missing a required ID field.");
        }

        var createdAt = response.CreatedAt.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(response.CreatedAt.Value)
            : DateTimeOffset.MinValue;

        var messageDto = new MessageDto(
            response.Id,
            response.Role ?? "unknown",
            createdAt,
            response.Model);

        var parts = response.Parts is { Count: > 0 }
            ? response.Parts.Select(p => new MessagePartDto(
                p.Type ?? "unknown",
                p.Text,
                p.ToolCallId,
                p.ToolName)).ToList()
            : [];

        return Result<MessageWithPartsDto>.Success(new MessageWithPartsDto(messageDto, parts));
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

    // ── Internal DTOs for JSON deserialization ──────────────────────────────

    /// <summary>
    /// Mirrors the JSON shape returned by the OpenCode Server for a message object.
    /// Timestamps are Unix milliseconds.
    /// </summary>
    private sealed record MessageResponse(
        string? Id,
        string? Role,
        long? CreatedAt,
        string? Model,
        List<PartResponse>? Parts);

    /// <summary>
    /// Mirrors the JSON shape returned by the OpenCode Server for a message part.
    /// </summary>
    private sealed record PartResponse(
        string? Type,
        string? Text,
        string? ToolCallId,
        string? ToolName);

    /// <summary>
    /// Request body for <c>POST /session/:id/message</c> and <c>POST /session/:id/prompt_async</c>.
    /// </summary>
    private sealed record SendMessageRequest(
        List<SendMessagePart> Parts,
        ModelOverride? Model,
        string? Agent);

    /// <summary>
    /// Represents a single part in a <see cref="SendMessageRequest"/>.
    /// </summary>
    private sealed record SendMessagePart(string Type, string Text);

    /// <summary>
    /// Represents an optional model override in a <see cref="SendMessageRequest"/>.
    /// </summary>
    private sealed record ModelOverride(string ProviderID, string ModelID);

    /// <summary>
    /// Request body for <c>POST /session/:id/command</c>.
    /// </summary>
    private sealed record CommandRequest(string Command, IReadOnlyList<string>? Args);
}
