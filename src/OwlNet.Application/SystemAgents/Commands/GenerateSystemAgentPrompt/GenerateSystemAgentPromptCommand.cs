using DispatchR.Abstractions.Send;
using OwlNet.Application.Agents.Models;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.GenerateSystemAgentPrompt;

/// <summary>
/// Command that orchestrates an LLM call for the system agent creation wizard's refinement
/// conversation. The LLM either returns follow-up questions to gather more context or generates
/// the final system agent definition markdown.
/// </summary>
public sealed record GenerateSystemAgentPromptCommand
    : IRequest<GenerateSystemAgentPromptCommand, ValueTask<Result<AgentGenerationResponseDto>>>
{
    /// <summary>
    /// The type of system agent being created — <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// The display name of the system agent being created.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// A free-text description of the system agent's purpose and capabilities.
    /// </summary>
    public required string AgentDescription { get; init; }

    /// <summary>
    /// The full question-and-answer conversation history so far. May be empty on the first call.
    /// </summary>
    public required IReadOnlyList<ConversationMessage> ConversationHistory { get; init; }

    /// <summary>
    /// When <c>true</c>, instructs the LLM to skip further questions and generate the system agent
    /// definition immediately with whatever information has been gathered so far.
    /// </summary>
    public required bool ForceGenerate { get; init; }
}
