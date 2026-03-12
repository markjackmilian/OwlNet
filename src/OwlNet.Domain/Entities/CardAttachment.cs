namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents an immutable Markdown document attached to a <see cref="Card"/> by an AI agent
/// during workflow trigger execution.
/// Attachments are append-only — once created they cannot be edited or deleted, ensuring a
/// reliable record of all agent-produced output for a card.
/// </summary>
public sealed class CardAttachment
{
    /// <summary>
    /// Gets the unique identifier for this attachment.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Card"/> this attachment belongs to.
    /// Immutable after creation.
    /// </summary>
    public Guid CardId { get; private set; }

    /// <summary>
    /// Gets the human-readable file name for this attachment (e.g., <c>code-review-summary.md</c>).
    /// Must be between 1 and 200 characters and must not be blank.
    /// Does not need to be unique within a card.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the full Markdown content of the attachment.
    /// No maximum length is enforced at the domain level.
    /// Must not be <see langword="null"/> or whitespace.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the agent that generated this attachment
    /// (the agent filename without the <c>.md</c> extension).
    /// Must be between 1 and 200 characters and must not be blank.
    /// </summary>
    public string AgentName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional foreign key of the <see cref="WorkflowTrigger"/> whose execution
    /// produced this attachment.
    /// Set at creation time.
    /// When the referenced <see cref="WorkflowTrigger"/> is deleted, this field is set to
    /// <see langword="null"/> (set-null behaviour) — the attachment itself is retained.
    /// </summary>
    public Guid? WorkflowTriggerId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this attachment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private CardAttachment() { }

    /// <summary>
    /// Creates a new <see cref="CardAttachment"/> with the specified properties.
    /// <see cref="Id"/> is generated automatically and <see cref="CreatedAt"/> is set to
    /// <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    /// <param name="cardId">The identifier of the <see cref="Card"/> this attachment belongs to.</param>
    /// <param name="fileName">
    /// The human-readable file name (e.g., <c>code-review-summary.md</c>).
    /// Must not be <see langword="null"/> or whitespace and must not exceed 200 characters.
    /// </param>
    /// <param name="content">
    /// The full Markdown content of the attachment.
    /// Must not be <see langword="null"/> or whitespace.
    /// </param>
    /// <param name="agentName">
    /// The name of the agent that generated the attachment (filename without <c>.md</c> extension).
    /// Must not be <see langword="null"/> or whitespace and must not exceed 200 characters.
    /// </param>
    /// <param name="workflowTriggerId">
    /// The identifier of the <see cref="WorkflowTrigger"/> whose execution produced this attachment.
    /// </param>
    /// <returns>A new, immutable <see cref="CardAttachment"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fileName"/> is <see langword="null"/>, whitespace, or exceeds
    /// 200 characters; when <paramref name="content"/> is <see langword="null"/> or whitespace;
    /// or when <paramref name="agentName"/> is <see langword="null"/>, whitespace, or exceeds
    /// 200 characters.
    /// </exception>
    public static CardAttachment Create(
        Guid cardId,
        string fileName,
        string content,
        string agentName,
        Guid workflowTriggerId)
    {
        ValidateFileName(fileName);
        ValidateContent(content);
        ValidateAgentName(agentName);

        return new CardAttachment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            FileName = fileName,
            Content = content,
            AgentName = agentName,
            WorkflowTriggerId = workflowTriggerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "Attachment file name must not be null or whitespace.",
                nameof(fileName));
        }

        if (fileName.Length > 200)
        {
            throw new ArgumentException(
                "Attachment file name must not exceed 200 characters.",
                nameof(fileName));
        }
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Attachment content must not be null or whitespace.",
                nameof(content));
        }
    }

    private static void ValidateAgentName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException(
                "Agent name must not be null or whitespace.",
                nameof(agentName));
        }

        if (agentName.Length > 200)
        {
            throw new ArgumentException(
                "Agent name must not exceed 200 characters.",
                nameof(agentName));
        }
    }
}
