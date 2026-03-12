using OwlNet.Domain.Enums;

namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents an immutable comment on a <see cref="Card"/>.
/// Comments are append-only — once created they cannot be edited or deleted, ensuring a
/// reliable audit trail of all activity on a card.
/// A comment is authored either by a human user (<see cref="CommentAuthorType.Human"/>) or
/// by an AI agent (<see cref="CommentAuthorType.Agent"/>) as part of a workflow trigger execution.
/// </summary>
public sealed class CardComment
{
    /// <summary>
    /// Gets the unique identifier for this comment.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Card"/> this comment belongs to.
    /// Immutable after creation.
    /// </summary>
    public Guid CardId { get; private set; }

    /// <summary>
    /// Gets the comment body. Supports Markdown.
    /// Must be between 1 and 10,000 characters and must not be blank.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the type of author that posted this comment.
    /// Determines which of <see cref="AuthorId"/> or <see cref="AgentName"/> is populated.
    /// </summary>
    public CommentAuthorType AuthorType { get; private set; }

    /// <summary>
    /// Gets the authenticated user's identifier when <see cref="AuthorType"/> is
    /// <see cref="CommentAuthorType.Human"/>.
    /// <see langword="null"/> when <see cref="AuthorType"/> is <see cref="CommentAuthorType.Agent"/>.
    /// </summary>
    public string? AuthorId { get; private set; }

    /// <summary>
    /// Gets the agent's identifier (the name of the <c>.md</c> agent file, without extension)
    /// when <see cref="AuthorType"/> is <see cref="CommentAuthorType.Agent"/>.
    /// <see langword="null"/> when <see cref="AuthorType"/> is <see cref="CommentAuthorType.Human"/>.
    /// </summary>
    public string? AgentName { get; private set; }

    /// <summary>
    /// Gets the optional foreign key of the <see cref="WorkflowTrigger"/> whose execution
    /// produced this comment.
    /// Set when an agent posts a comment as part of a trigger execution.
    /// <see langword="null"/> for human comments and for agent comments posted outside a trigger context.
    /// When the referenced <see cref="WorkflowTrigger"/> is deleted, this field is set to
    /// <see langword="null"/> (set-null behaviour) — the comment itself is retained.
    /// </summary>
    public Guid? WorkflowTriggerId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this comment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private CardComment() { }

    /// <summary>
    /// Creates a new <see cref="CardComment"/> with the specified properties.
    /// <see cref="Id"/> is generated automatically and <see cref="CreatedAt"/> is set to
    /// <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    /// <param name="cardId">The identifier of the <see cref="Card"/> being commented on.</param>
    /// <param name="content">
    /// The comment body. Must not be <see langword="null"/> or whitespace and must not exceed
    /// 10,000 characters. Supports Markdown.
    /// </param>
    /// <param name="authorType">
    /// Whether the comment is authored by a human user or an AI agent.
    /// </param>
    /// <param name="authorId">
    /// The authenticated user's identifier. Required (must not be <see langword="null"/> or
    /// whitespace) when <paramref name="authorType"/> is <see cref="CommentAuthorType.Human"/>.
    /// Must be <see langword="null"/> when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Agent"/>.
    /// </param>
    /// <param name="agentName">
    /// The agent's identifier (the name of the <c>.md</c> agent file, without extension).
    /// Required (must not be <see langword="null"/> or whitespace) when
    /// <paramref name="authorType"/> is <see cref="CommentAuthorType.Agent"/>.
    /// Must be <see langword="null"/> when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Human"/>.
    /// </param>
    /// <param name="workflowTriggerId">
    /// The optional identifier of the <see cref="WorkflowTrigger"/> whose execution produced
    /// this comment. Pass <see langword="null"/> for human comments or agent comments posted
    /// outside a trigger context.
    /// </param>
    /// <returns>A new, immutable <see cref="CardComment"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is <see langword="null"/>, whitespace, or exceeds
    /// 10,000 characters; when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Human"/> and <paramref name="authorId"/> is
    /// <see langword="null"/> or whitespace; or when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Agent"/> and <paramref name="agentName"/> is
    /// <see langword="null"/> or whitespace.
    /// </exception>
    public static CardComment Create(
        Guid cardId,
        string content,
        CommentAuthorType authorType,
        string? authorId = null,
        string? agentName = null,
        Guid? workflowTriggerId = null)
    {
        ValidateContent(content);
        ValidateAuthorFields(authorType, authorId, agentName);

        return new CardComment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Content = content,
            AuthorType = authorType,
            AuthorId = authorId,
            AgentName = agentName,
            WorkflowTriggerId = workflowTriggerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (content.Length > 10_000)
        {
            throw new ArgumentException("Comment content must not exceed 10,000 characters.", nameof(content));
        }
    }

    private static void ValidateAuthorFields(CommentAuthorType authorType, string? authorId, string? agentName)
    {
        if (authorType == CommentAuthorType.Human)
        {
            if (string.IsNullOrWhiteSpace(authorId))
            {
                throw new ArgumentException(
                    "Author ID is required for human comments.",
                    nameof(authorId));
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(agentName))
            {
                throw new ArgumentException(
                    "Agent name is required for agent comments.",
                    nameof(agentName));
            }
        }
    }
}
