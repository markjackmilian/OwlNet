using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddCardAttachment;

/// <summary>
/// Command to add an agent-generated Markdown attachment to an existing card.
/// Attachments are always produced by AI agents during workflow trigger execution — manual upload is not supported.
/// </summary>
public sealed record AddCardAttachmentCommand
    : IRequest<AddCardAttachmentCommand, ValueTask<Result<Guid>>>
{
    /// <summary>The identifier of the card to attach the document to.</summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The human-readable file name (e.g., <c>code-review-summary.md</c>).
    /// Must not be blank and must not exceed 200 characters.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The full Markdown content of the attachment. Must not be blank.
    /// No maximum length is enforced at the application layer.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The name of the agent that generated the attachment (filename without <c>.md</c> extension).
    /// Must not be blank and must not exceed 200 characters.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// The identifier of the <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> whose execution produced this attachment.
    /// Must reference an existing workflow trigger.
    /// </summary>
    public required Guid WorkflowTriggerId { get; init; }
}
