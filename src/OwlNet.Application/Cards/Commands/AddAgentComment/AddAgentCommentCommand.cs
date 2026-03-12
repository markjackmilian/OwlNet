using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddAgentComment;

/// <summary>
/// Command to add an AI agent-authored comment to an existing card.
/// The comment is attributed to the agent identified by <see cref="AgentName"/> and may
/// optionally be linked to a workflow trigger execution via <see cref="WorkflowTriggerId"/>.
/// </summary>
public sealed record AddAgentCommentCommand
    : IRequest<AddAgentCommentCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The identifier of the card to comment on.
    /// </summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The comment body. Supports Markdown. Must be between 1 and 10,000 characters.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The agent's identifier — the name of the <c>.md</c> agent file, without extension.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// The optional identifier of the <c>WorkflowTrigger</c> whose execution produced this comment.
    /// <see langword="null"/> when the comment is posted outside a trigger context.
    /// </summary>
    public Guid? WorkflowTriggerId { get; init; }
}
