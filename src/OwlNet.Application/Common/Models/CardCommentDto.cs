using OwlNet.Domain.Enums;

namespace OwlNet.Application.Common.Models;

/// <summary>
/// Read-only projection of a <see cref="OwlNet.Domain.Entities.CardComment"/> record,
/// including a denormalised <see cref="WorkflowTriggerName"/> resolved from the related
/// <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> (null-safe: returns <see langword="null"/>
/// when the trigger has been deleted or the comment was not posted via a trigger).
/// </summary>
/// <param name="Id">The unique identifier of the comment.</param>
/// <param name="CardId">The identifier of the card this comment belongs to.</param>
/// <param name="Content">The raw Markdown content of the comment.</param>
/// <param name="AuthorType">
/// Indicates whether the comment was written by a human user or posted by an AI agent.
/// </param>
/// <param name="AuthorId">
/// The authenticated user's identifier when <paramref name="AuthorType"/> is
/// <see cref="CommentAuthorType.Human"/>; <see langword="null"/> for agent comments.
/// </param>
/// <param name="AgentName">
/// The agent's identifier (name of the <c>.md</c> agent file, without extension) when
/// <paramref name="AuthorType"/> is <see cref="CommentAuthorType.Agent"/>;
/// <see langword="null"/> for human comments.
/// </param>
/// <param name="WorkflowTriggerId">
/// The identifier of the <see cref="OwlNet.Domain.Entities.WorkflowTrigger"/> that caused this
/// comment to be posted, or <see langword="null"/> if the comment was not posted as part of a
/// trigger execution.
/// </param>
/// <param name="WorkflowTriggerName">
/// The denormalised display name of the workflow trigger (resolved at query time).
/// <see langword="null"/> when <paramref name="WorkflowTriggerId"/> is <see langword="null"/>,
/// or when the trigger has since been deleted.
/// </param>
/// <param name="CreatedAt">The UTC timestamp when the comment was created.</param>
public sealed record CardCommentDto(
    Guid Id,
    Guid CardId,
    string Content,
    CommentAuthorType AuthorType,
    string? AuthorId,
    string? AgentName,
    Guid? WorkflowTriggerId,
    string? WorkflowTriggerName,
    DateTimeOffset CreatedAt);
