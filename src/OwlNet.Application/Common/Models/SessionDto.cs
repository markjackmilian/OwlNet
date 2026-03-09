namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents an OpenCode session (conversation thread with the AI agent).
/// Maps the relevant fields from the OpenCode Server <c>Session</c> type.
/// </summary>
/// <param name="Id">The unique session identifier assigned by the OpenCode Server.</param>
/// <param name="Title">An optional human-readable title for the session, or <c>null</c> if not set.</param>
/// <param name="CreatedAt">The timestamp when the session was created.</param>
/// <param name="UpdatedAt">The timestamp when the session was last updated.</param>
public sealed record SessionDto(
    string Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
