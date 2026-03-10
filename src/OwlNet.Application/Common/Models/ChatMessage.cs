namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents a single message in a chat conversation with an LLM.
/// </summary>
/// <param name="Role">The role of the message author (e.g., "user", "assistant", "system").</param>
/// <param name="Content">The text content of the message.</param>
public sealed record ChatMessage(string Role, string Content);
