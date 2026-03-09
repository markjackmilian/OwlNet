namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the execution status of an OpenCode session.
/// Maps the status field from the OpenCode Server <c>GET /session/status</c> response.
/// </summary>
/// <param name="Status">
/// The current status string (e.g., <c>"idle"</c>, <c>"running"</c>, <c>"error"</c>).
/// </param>
public sealed record SessionStatusDto(string Status);
