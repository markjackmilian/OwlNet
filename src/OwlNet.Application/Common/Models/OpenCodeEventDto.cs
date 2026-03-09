using System.Text.Json;

namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents a parsed Server-Sent Event (SSE) received from the OpenCode Server.
/// Maps the <c>event:</c> and <c>data:</c> fields of the SSE stream into a typed record.
/// </summary>
/// <param name="Type">The SSE event type (e.g., <c>"session.updated"</c>, <c>"message.updated"</c>).</param>
/// <param name="Timestamp">The timestamp when the event was received and parsed by the client.</param>
/// <param name="Data">The deserialized JSON payload from the SSE <c>data:</c> field, or <c>null</c> if absent.</param>
public sealed record OpenCodeEventDto(
    string Type,
    DateTimeOffset Timestamp,
    JsonElement? Data);
