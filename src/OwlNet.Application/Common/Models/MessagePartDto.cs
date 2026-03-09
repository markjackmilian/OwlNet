namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents a part of an OpenCode message.
/// Messages are composed of one or more parts, each with a specific type and optional content.
/// </summary>
/// <param name="Type">The part type (e.g., <c>"text"</c>, <c>"tool_call"</c>, <c>"tool_result"</c>).</param>
/// <param name="Content">The text content of the part, or <c>null</c> for non-text parts.</param>
/// <param name="ToolCallId">The tool call identifier, or <c>null</c> for non-tool parts.</param>
/// <param name="ToolName">The name of the tool, or <c>null</c> for non-tool-call parts.</param>
public sealed record MessagePartDto(
    string Type,
    string? Content,
    string? ToolCallId,
    string? ToolName);
