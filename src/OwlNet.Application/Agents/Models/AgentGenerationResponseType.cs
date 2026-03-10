namespace OwlNet.Application.Agents.Models;

/// <summary>
/// Indicates the type of response returned by the agent generation LLM call.
/// </summary>
public enum AgentGenerationResponseType
{
    /// <summary>The assistant is asking follow-up questions to refine the agent definition.</summary>
    Questions,

    /// <summary>The assistant has produced the final agent markdown definition.</summary>
    GeneratedMarkdown
}
