namespace OwlNet.Application.Agents.Models;

/// <summary>
/// Represents the parsed response from the agent generation LLM call,
/// containing either follow-up questions or the final generated agent markdown.
/// </summary>
/// <param name="ResponseType">The type of response — questions or generated markdown.</param>
/// <param name="Questions">
/// The list of follow-up questions when <paramref name="ResponseType"/> is
/// <see cref="AgentGenerationResponseType.Questions"/>; <c>null</c> otherwise.
/// </param>
/// <param name="GeneratedMarkdown">
/// The final agent definition markdown when <paramref name="ResponseType"/> is
/// <see cref="AgentGenerationResponseType.GeneratedMarkdown"/>; <c>null</c> otherwise.
/// </param>
/// <param name="AssistantMessage">The full assistant message text for display in the conversation UI.</param>
public sealed record AgentGenerationResponseDto(
    AgentGenerationResponseType ResponseType,
    IReadOnlyList<string>? Questions,
    string? GeneratedMarkdown,
    string AssistantMessage);
