using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Manages the lifecycle of the OpenCode Server process.
/// Registered as a singleton — the server is a single global process shared across all users/circuits.
/// </summary>
public interface IOpenCodeServerManager
{
    /// <summary>
    /// Gets the current status of the OpenCode Server.
    /// </summary>
    OpenCodeServerStatus CurrentStatus { get; }

    /// <summary>
    /// Event raised when the server status changes. Subscribers should use
    /// <c>InvokeAsync(StateHasChanged)</c> in Blazor components.
    /// </summary>
    event Action? OnStatusChanged;

    /// <summary>
    /// Starts the OpenCode Server process. Reads the configured URL from settings
    /// to determine the port. If the server is already running, returns success without action.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> StartServerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the OpenCode Server process. If the server is not running, returns success without action.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> StopServerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check against the currently configured server URL and updates the status.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated <see cref="OpenCodeServerStatus"/>.</returns>
    Task<OpenCodeServerStatus> RefreshStatusAsync(CancellationToken cancellationToken = default);
}
