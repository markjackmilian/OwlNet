using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IOpenCodeClient"/> that communicates with the
/// OpenCode Server API over HTTP. Uses a typed <see cref="HttpClient"/> (via
/// <see cref="IHttpClientFactory"/>) with no fixed base address — the server URL is resolved
/// at request time to support runtime configuration changes without restarting the application.
/// All methods return result objects and never throw exceptions to the caller (except when
/// the caller's own <see cref="CancellationToken"/> is cancelled).
/// </summary>
public sealed class OpenCodeClient : IOpenCodeClient
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCodeClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeClient"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client configured by <see cref="IHttpClientFactory"/>. Must have a default
    /// timeout set at DI registration time (typically 30 seconds) but no <c>BaseAddress</c>.
    /// </param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public OpenCodeClient(HttpClient httpClient, ILogger<OpenCodeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<OpenCodeHealthResult>> HealthCheckAsync(
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing OpenCode Server health check against {BaseUrl}", baseUrl);

        Uri healthUri;
        try
        {
            healthUri = new Uri(baseUrl.TrimEnd('/') + "/global/health");
        }
        catch (UriFormatException)
        {
            _logger.LogWarning("Malformed OpenCode Server URL: {BaseUrl}", baseUrl);
            return Result<OpenCodeHealthResult>.Failure("Invalid server URL format.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(healthUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogWarning(
                    "OpenCode Server health check returned non-success status {StatusCode} from {BaseUrl}",
                    statusCode,
                    baseUrl);
                return Result<OpenCodeHealthResult>.Failure($"Server error ({statusCode})");
            }

            var healthResponse = await response.Content.ReadFromJsonAsync<OpenCodeHealthResponse>(
                CamelCaseOptions,
                cancellationToken);

            if (healthResponse is null)
            {
                _logger.LogWarning(
                    "OpenCode Server health check returned null response body from {BaseUrl}",
                    baseUrl);
                return Result<OpenCodeHealthResult>.Failure("Unexpected response format");
            }

            var result = new OpenCodeHealthResult(healthResponse.Healthy, healthResponse.Version);

            _logger.LogInformation(
                "OpenCode Server health check succeeded (Healthy: {IsHealthy}, Version: {Version})",
                result.IsHealthy,
                result.Version);

            return Result<OpenCodeHealthResult>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller-initiated cancellation — rethrow so the caller can handle it.
            throw;
        }
        catch (TaskCanceledException)
        {
            // HttpClient timeout (not caller cancellation, which is handled above).
            _logger.LogWarning(
                "OpenCode Server health check timed out for {BaseUrl}",
                baseUrl);
            return Result<OpenCodeHealthResult>.Failure("Request timed out");
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(
                ex,
                "OpenCode Server health check connection refused for {BaseUrl}",
                baseUrl);
            return Result<OpenCodeHealthResult>.Failure("Connection refused");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "OpenCode Server health check network error for {BaseUrl}",
                baseUrl);
            return Result<OpenCodeHealthResult>.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "OpenCode Server health check returned unparseable response from {BaseUrl}",
                baseUrl);
            return Result<OpenCodeHealthResult>.Failure("Unexpected response format");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred during OpenCode Server health check for {BaseUrl}",
                baseUrl);
            return Result<OpenCodeHealthResult>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal DTO that mirrors the JSON shape returned by <c>GET /global/health</c>.
    /// </summary>
    private sealed record OpenCodeHealthResponse(bool Healthy, string? Version);
}
