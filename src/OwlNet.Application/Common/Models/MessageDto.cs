namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents an OpenCode message with metadata.
/// Maps the relevant fields from the OpenCode Server <c>Message</c> type.
/// </summary>
/// <param name="Id">The unique message identifier assigned by the OpenCode Server.</param>
/// <param name="Role">The role of the message sender (e.g., <c>"user"</c>, <c>"assistant"</c>).</param>
/// <param name="CreatedAt">The timestamp when the message was created.</param>
/// <param name="Model">The model identifier that generated the response, or <c>null</c> for user messages.</param>
public sealed record MessageDto(
    string Id,
    string Role,
    DateTimeOffset CreatedAt,
    string? Model);
