using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Service for managing OpenCode sessions (conversation threads with the AI agent).
/// All methods accept the server base URL as a parameter to support runtime URL changes.
/// </summary>
public interface IOpenCodeSessionService
{
    /// <summary>
    /// Creates a new session by calling <c>POST /session</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="title">An optional human-readable title for the new session.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created <see cref="SessionDto"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<SessionDto>> CreateSessionAsync(string baseUrl, string? title = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sessions by calling <c>GET /session</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a read-only list of <see cref="SessionDto"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<IReadOnlyList<SessionDto>>> ListSessionsAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the details of a specific session by calling <c>GET /session/:id</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the <see cref="SessionDto"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<SessionDto>> GetSessionAsync(string baseUrl, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session by calling <c>DELETE /session/:id</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure with error details.
    /// </returns>
    Task<Result> DeleteSessionAsync(string baseUrl, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts a running session by calling <c>POST /session/:id/abort</c>.
    /// This operation is idempotent — aborting a session that is not running returns success.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to abort.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure with error details.
    /// </returns>
    Task<Result> AbortSessionAsync(string baseUrl, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of a session (e.g., title) by calling <c>PATCH /session/:id</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="sessionId">The unique identifier of the session to update.</param>
    /// <param name="title">The new title for the session.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the updated <see cref="SessionDto"/> on success,
    /// or a failure result with error details on failure.
    /// </returns>
    Task<Result<SessionDto>> UpdateSessionAsync(string baseUrl, string sessionId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the execution status of all sessions by calling <c>GET /session/status</c>.
    /// </summary>
    /// <param name="baseUrl">
    /// The base URL of the OpenCode Server (e.g., <c>"http://127.0.0.1:4096"</c>).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a read-only dictionary mapping session IDs
    /// to their <see cref="SessionStatusDto"/> on success, or a failure result with error details on failure.
    /// </returns>
    Task<Result<IReadOnlyDictionary<string, SessionStatusDto>>> GetSessionStatusesAsync(string baseUrl, CancellationToken cancellationToken = default);
}
