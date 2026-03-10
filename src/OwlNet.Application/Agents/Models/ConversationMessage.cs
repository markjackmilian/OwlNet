namespace OwlNet.Application.Agents.Models;

/// <summary>
/// Represents a single message in the agent creation wizard conversation.
/// </summary>
/// <param name="Role">The message role — <c>"assistant"</c> or <c>"user"</c>.</param>
/// <param name="Content">The text content of the message.</param>
public sealed record ConversationMessage(
    string Role,
    string Content);
