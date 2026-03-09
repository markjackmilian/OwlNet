namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the result of an OpenCode Server health check.
/// Maps the response from <c>GET /global/health</c>.
/// </summary>
/// <param name="IsHealthy">Whether the server reported itself as healthy.</param>
/// <param name="Version">The server version string (e.g., "1.2.20"), or null if unavailable.</param>
public sealed record OpenCodeHealthResult(bool IsHealthy, string? Version);
