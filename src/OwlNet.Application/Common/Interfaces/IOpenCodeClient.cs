using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// HTTP client for communicating with the OpenCode Server API.
/// All methods accept the server base URL as a parameter to support runtime URL changes.
/// </summary>
public interface IOpenCodeClient
{
    /// <summary>
    /// Performs a health check against the OpenCode Server by calling <c>GET /global/health</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing an <see cref="OpenCodeHealthResult"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<OpenCodeHealthResult>> HealthCheckAsync(string baseUrl, CancellationToken cancellationToken = default);
}
