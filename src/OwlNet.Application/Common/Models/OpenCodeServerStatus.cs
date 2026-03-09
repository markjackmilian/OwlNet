namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the current status of the OpenCode Server, including process state,
/// health information, and the URL it was last known to be running on.
/// </summary>
/// <param name="State">The current process state.</param>
/// <param name="Version">The server version if healthy, null otherwise.</param>
/// <param name="ServerUrl">The URL the server is running on (or was last configured with).</param>
/// <param name="ErrorMessage">Error details when <see cref="State"/> is <see cref="OpenCodeServerState.Error"/>, null otherwise.</param>
public sealed record OpenCodeServerStatus(
    OpenCodeServerState State,
    string? Version,
    string? ServerUrl,
    string? ErrorMessage)
{
    /// <summary>
    /// Gets whether the server is healthy and responding.
    /// </summary>
    public bool IsHealthy => State == OpenCodeServerState.Running && Version is not null;

    /// <summary>
    /// Creates an <see cref="OpenCodeServerState.Unknown"/> status (initial state).
    /// </summary>
    /// <returns>A status representing an unknown server state.</returns>
    public static OpenCodeServerStatus CreateUnknown() =>
        new(OpenCodeServerState.Unknown, null, null, null);

    /// <summary>
    /// Creates a <see cref="OpenCodeServerState.Starting"/> status.
    /// </summary>
    /// <param name="serverUrl">The URL the server is being started on.</param>
    /// <returns>A status representing a server that is starting up.</returns>
    public static OpenCodeServerStatus CreateStarting(string serverUrl) =>
        new(OpenCodeServerState.Starting, null, serverUrl, null);

    /// <summary>
    /// Creates a <see cref="OpenCodeServerState.Running"/> status with version info.
    /// </summary>
    /// <param name="version">The server version string from the health check.</param>
    /// <param name="serverUrl">The URL the server is running on.</param>
    /// <returns>A status representing a healthy, running server.</returns>
    public static OpenCodeServerStatus CreateRunning(string version, string serverUrl) =>
        new(OpenCodeServerState.Running, version, serverUrl, null);

    /// <summary>
    /// Creates a <see cref="OpenCodeServerState.Stopped"/> status.
    /// </summary>
    /// <returns>A status representing a stopped server.</returns>
    public static OpenCodeServerStatus CreateStopped() =>
        new(OpenCodeServerState.Stopped, null, null, null);

    /// <summary>
    /// Creates a <see cref="OpenCodeServerState.Stopping"/> status.
    /// </summary>
    /// <returns>A status representing a server that is being stopped.</returns>
    public static OpenCodeServerStatus CreateStopping() =>
        new(OpenCodeServerState.Stopping, null, null, null);

    /// <summary>
    /// Creates an <see cref="OpenCodeServerState.Error"/> status with details.
    /// </summary>
    /// <param name="errorMessage">A description of the error that occurred.</param>
    /// <returns>A status representing a server in an error state.</returns>
    public static OpenCodeServerStatus CreateError(string errorMessage) =>
        new(OpenCodeServerState.Error, null, null, errorMessage);
}
