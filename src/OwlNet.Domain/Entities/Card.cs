using OwlNet.Domain.Common;
using OwlNet.Domain.Enums;

namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a work item (card) on the Kanban board.
/// A card belongs to exactly one <see cref="Project"/> and references one of that project's
/// <see cref="BoardStatus"/> entries as its current status.
/// Every status transition — including the initial assignment at creation — is recorded
/// as an immutable <see cref="CardStatusHistory"/> entry.
/// </summary>
public sealed class Card
{
    private readonly List<CardStatusHistory> _statusHistory = [];
    private readonly List<CardTag> _tags = [];
    private readonly List<CardComment> _comments = [];

    /// <summary>
    /// Gets the unique identifier for this card.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the human-readable sequential number of this card within its project.
    /// Assigned by the caller (typically the repository) at creation time and immutable thereafter.
    /// Example: 1, 2, 3 — displayed as "PROJ-1", "PROJ-2", etc. in the UI.
    /// </summary>
    public int Number { get; private set; }

    /// <summary>
    /// Gets the card title. Must be between 1 and 200 characters and must not be blank.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional card description. Supports Markdown.
    /// May be an empty string but never <see langword="null"/>.
    /// Maximum 5000 characters.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the priority level of this card.
    /// </summary>
    public CardPriority Priority { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="BoardStatus"/> this card is currently in.
    /// Must belong to the same project as this card.
    /// </summary>
    public Guid StatusId { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Project"/> this card belongs to.
    /// Immutable after creation.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this card was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this card was last modified.
    /// Updated on every mutation (title/description/priority change or status change).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the read-only list of status transitions recorded for this card,
    /// in the order they were appended (oldest first).
    /// Includes the initial status assignment at creation (with <c>PreviousStatusId = null</c>).
    /// </summary>
    public IReadOnlyList<CardStatusHistory> StatusHistory => _statusHistory;

    /// <summary>
    /// Gets the read-only list of tags assigned to this card.
    /// </summary>
    public IReadOnlyList<CardTag> Tags => _tags;

    /// <summary>
    /// Gets the read-only list of comments posted on this card,
    /// in the order they were appended (oldest first).
    /// </summary>
    public IReadOnlyList<CardComment> Comments => _comments;

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private Card() { }

    /// <summary>
    /// Creates a new <see cref="Card"/> with the specified properties and records the initial
    /// status assignment in <see cref="StatusHistory"/>.
    /// </summary>
    /// <param name="title">
    /// The card title. Must not be <see langword="null"/> or whitespace and must be between
    /// 1 and 200 characters.
    /// </param>
    /// <param name="description">
    /// An optional description supporting Markdown. A <see langword="null"/> value is coerced to
    /// <see cref="string.Empty"/>. Must not exceed 5000 characters.
    /// </param>
    /// <param name="priority">The initial priority level of the card.</param>
    /// <param name="statusId">
    /// The identifier of the <see cref="BoardStatus"/> to assign the card to.
    /// Must belong to the project identified by <paramref name="projectId"/>.
    /// </param>
    /// <param name="projectId">The identifier of the owning <see cref="Project"/>.</param>
    /// <param name="number">
    /// The sequential number of this card within the project, assigned by the caller.
    /// </param>
    /// <param name="createdBy">
    /// The identifier of the user creating the card. Must not be <see langword="null"/> or whitespace.
    /// Stored as the <c>ChangedBy</c> value on the initial <see cref="CardStatusHistory"/> record.
    /// </param>
    /// <returns>A new <see cref="Card"/> instance with one initial history record.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is <see langword="null"/>, whitespace, empty,
    /// or exceeds 200 characters; when <paramref name="description"/> exceeds 5000 characters;
    /// or when <paramref name="createdBy"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public static Card Create(
        string title,
        string? description,
        CardPriority priority,
        Guid statusId,
        Guid projectId,
        int number,
        string createdBy)
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var now = DateTimeOffset.UtcNow;

        var card = new Card
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Priority = priority,
            StatusId = statusId,
            ProjectId = projectId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var initialHistory = CardStatusHistory.Create(
            cardId: card.Id,
            previousStatusId: null,
            newStatusId: statusId,
            changedBy: createdBy,
            changeSource: ChangeSource.Manual);

        card._statusHistory.Add(initialHistory);

        return card;
    }

    /// <summary>
    /// Updates the card's title, description, and priority, and refreshes the
    /// <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="title">
    /// The new title. Must not be <see langword="null"/> or whitespace and must be between
    /// 1 and 200 characters.
    /// </param>
    /// <param name="description">
    /// The new description. A <see langword="null"/> value is coerced to <see cref="string.Empty"/>.
    /// Must not exceed 5000 characters.
    /// </param>
    /// <param name="priority">The new priority level.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="title"/> is <see langword="null"/>, whitespace, empty,
    /// or exceeds 200 characters; or when <paramref name="description"/> exceeds 5000 characters.
    /// </exception>
    public void Update(string title, string? description, CardPriority priority)
    {
        ValidateTitle(title);
        ValidateDescription(description);

        Title = title.Trim();
        Description = description ?? string.Empty;
        Priority = priority;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Transitions the card to a new <see cref="BoardStatus"/> and records the change in
    /// <see cref="StatusHistory"/>.
    /// </summary>
    /// <param name="newStatusId">The identifier of the target <see cref="BoardStatus"/>.</param>
    /// <param name="newStatusProjectId">
    /// The <c>ProjectId</c> of the target <see cref="BoardStatus"/>.
    /// Must equal <see cref="ProjectId"/> — cross-project status assignments are rejected.
    /// </param>
    /// <param name="changedBy">
    /// The identifier of the actor performing the change (user ID or agent/trigger identifier).
    /// Must not be <see langword="null"/> or whitespace.
    /// </param>
    /// <param name="changeSource">Whether the change was performed manually or by automation.</param>
    /// <returns>
    /// <see cref="DomainResult.Success"/> when the status was changed or when
    /// <paramref name="newStatusId"/> equals the current <see cref="StatusId"/> (no-op).
    /// <see cref="DomainResult.Failure"/> when <paramref name="newStatusProjectId"/> does not
    /// match <see cref="ProjectId"/>.
    /// </returns>
    public DomainResult ChangeStatus(
        Guid newStatusId,
        Guid newStatusProjectId,
        string changedBy,
        ChangeSource changeSource)
    {
        // No-op: already in the requested status — no history record, no error.
        if (newStatusId == StatusId)
        {
            return DomainResult.Success();
        }

        // Guard: the target status must belong to this card's project.
        if (newStatusProjectId != ProjectId)
        {
            return DomainResult.Failure("Status does not belong to this project.");
        }

        var history = CardStatusHistory.Create(
            cardId: Id,
            previousStatusId: StatusId,
            newStatusId: newStatusId,
            changedBy: changedBy,
            changeSource: changeSource);

        _statusHistory.Add(history);

        StatusId = newStatusId;
        UpdatedAt = DateTimeOffset.UtcNow;

        return DomainResult.Success();
    }

    /// <summary>
    /// Assigns a <see cref="ProjectTag"/> to this card.
    /// </summary>
    /// <param name="tagId">The identifier of the <see cref="ProjectTag"/> to assign.</param>
    /// <param name="tagProjectId">
    /// The <c>ProjectId</c> of the <see cref="ProjectTag"/> being assigned.
    /// Must equal <see cref="ProjectId"/> — cross-project tag assignments are rejected.
    /// </param>
    /// <returns>
    /// <see cref="DomainResult.Success"/> when the tag was added or when it was already present
    /// (idempotent — no duplicate entry is created).
    /// <see cref="DomainResult.Failure"/> when <paramref name="tagProjectId"/> does not match
    /// <see cref="ProjectId"/>.
    /// </returns>
    public DomainResult AddTag(Guid tagId, Guid tagProjectId)
    {
        // Guard: the tag must belong to this card's project.
        if (tagProjectId != ProjectId)
        {
            return DomainResult.Failure("Tag does not belong to this project.");
        }

        // Idempotent: tag already present — no-op, no error.
        if (_tags.Any(t => t.TagId == tagId))
        {
            return DomainResult.Success();
        }

        _tags.Add(CardTag.Create(Id, tagId));
        UpdatedAt = DateTimeOffset.UtcNow;

        return DomainResult.Success();
    }

    /// <summary>
    /// Removes a <see cref="ProjectTag"/> from this card.
    /// This method is a no-op when the tag is not currently assigned to the card.
    /// </summary>
    /// <param name="tagId">The identifier of the <see cref="ProjectTag"/> to remove.</param>
    public void RemoveTag(Guid tagId)
    {
        var existing = _tags.FirstOrDefault(t => t.TagId == tagId);

        if (existing is null)
        {
            return;
        }

        _tags.Remove(existing);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new <see cref="CardComment"/> on this card and appends it to <see cref="Comments"/>.
    /// All validation is delegated to <see cref="CardComment.Create"/>; no validation logic is
    /// duplicated here.
    /// </summary>
    /// <remarks>
    /// This method does <b>not</b> update <see cref="UpdatedAt"/> — comments are a separate concern
    /// from card data and do not constitute a modification to the card itself.
    /// </remarks>
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
    /// The optional identifier of the workflow trigger whose execution produced this comment.
    /// Pass <see langword="null"/> for human comments or agent comments posted outside a trigger context.
    /// </param>
    /// <returns>The newly created <see cref="CardComment"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is <see langword="null"/>, whitespace, or exceeds
    /// 10,000 characters; when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Human"/> and <paramref name="authorId"/> is
    /// <see langword="null"/> or whitespace; or when <paramref name="authorType"/> is
    /// <see cref="CommentAuthorType.Agent"/> and <paramref name="agentName"/> is
    /// <see langword="null"/> or whitespace.
    /// </exception>
    public CardComment AddComment(
        string content,
        CommentAuthorType authorType,
        string? authorId = null,
        string? agentName = null,
        Guid? workflowTriggerId = null)
    {
        var comment = CardComment.Create(Id, content, authorType, authorId, agentName, workflowTriggerId);

        _comments.Add(comment);

        return comment;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Card title must not be null or whitespace.", nameof(title));
        }

        if (title.Length > 200)
        {
            throw new ArgumentException("Card title must not exceed 200 characters.", nameof(title));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > 5000)
        {
            throw new ArgumentException("Card description must not exceed 5000 characters.", nameof(description));
        }
    }
}
