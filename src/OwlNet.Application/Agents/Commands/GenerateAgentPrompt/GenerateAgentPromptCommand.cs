using DispatchR.Abstractions.Send;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Agents.Commands.GenerateAgentPrompt;

/// <summary>
/// Command that orchestrates an LLM call for the agent creation wizard's refinement conversation.
/// The LLM either returns follow-up questions to gather more context or generates the final
/// agent definition markdown.
/// </summary>
public sealed record GenerateAgentPromptCommand
    : IRequest<GenerateAgentPromptCommand, ValueTask<Result<AgentGenerationResponseDto>>>
{
    /// <summary>
    /// The type of agent being created — <c>"primary"</c> or <c>"subagent"</c>.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// The display name of the agent being created.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// A free-text description of the agent's purpose and capabilities.
    /// </summary>
    public required string AgentDescription { get; init; }

    /// <summary>
    /// The full question-and-answer conversation history so far. May be empty on the first call.
    /// </summary>
    public required IReadOnlyList<ConversationMessage> ConversationHistory { get; init; }

    /// <summary>
    /// When <c>true</c>, instructs the LLM to skip further questions and generate the agent
    /// definition immediately with whatever information has been gathered so far.
    /// </summary>
    public required bool ForceGenerate { get; init; }
}
