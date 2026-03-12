namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.CardAttachment"/> record,
/// including a denormalised <see cref="WorkflowTriggerName"/> resolved from the related
/// <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> (null-safe: returns <see langword="null"/>
/// when the trigger has been deleted).
/// </summary>
/// <remarks>
/// <see cref="OwlNet.Domain.Entities.CardAttachment.Content"/> is intentionally excluded from
/// this DTO to keep list-view queries lightweight. Use
/// <c>GetCardAttachmentContentQuery</c> to fetch the full content of a single attachment on demand.
/// </remarks>
/// <param name="Id">The unique identifier of the attachment.</param>
/// <param name="CardId">The identifier of the card this attachment belongs to.</param>
/// <param name="FileName">
/// The human-readable file name of the attachment (e.g., <c>code-review-summary.md</c>).
/// </param>
/// <param name="AgentName">
/// The name of the agent that generated the attachment (filename without <c>.md</c> extension).
/// </param>
/// <param name="WorkflowTriggerId">
/// The identifier of the <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> that caused this
/// attachment to be created, or <see langword="null"/> if the trigger has since been deleted.
/// </param>
/// <param name="WorkflowTriggerName">
/// The denormalised display name of the workflow trigger (resolved at query time).
/// <see langword="null"/> when <paramref name="WorkflowTriggerId"/> is <see langword="null"/>,
/// or when the trigger has since been deleted.
/// </param>
/// <param name="CreatedAt">The UTC timestamp when the attachment was created.</param>
public sealed record CardAttachmentDto(
    Guid Id,
    Guid CardId,
    string FileName,
    string AgentName,
    Guid? WorkflowTriggerId,
    string? WorkflowTriggerName,
    DateTimeOffset CreatedAt);
