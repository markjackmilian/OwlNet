using OwlNet.Domain.Enums;

namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents an immutable record of a single status transition on a <see cref="Card"/>.
/// Every status change — including the initial assignment at card creation — produces one record.
/// Records are append-only and must never be updated or deleted independently
/// (they are cascade-deleted when the owning card is deleted).
/// </summary>
public sealed class CardStatusHistory
{
    /// <summary>
    /// Gets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Card"/> that owns this history record.
    /// </summary>
    public Guid CardId { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="BoardStatus"/> the card was in before this transition.
    /// <see langword="null"/> for the initial status assignment at card creation.
    /// </summary>
    public Guid? PreviousStatusId { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="BoardStatus"/> the card transitioned into.
    /// </summary>
    public Guid NewStatusId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of when this status transition occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the actor that performed the change.
    /// Contains the user ID for manual changes or an agent/trigger identifier for automated changes.
    /// </summary>
    public string ChangedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the source that triggered this status change.
    /// </summary>
    public ChangeSource ChangeSource { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private CardStatusHistory() { }

    /// <summary>
    /// Creates a new <see cref="CardStatusHistory"/> record representing a status transition.
    /// </summary>
    /// <param name="cardId">The identifier of the card that changed status.</param>
    /// <param name="previousStatusId">
    /// The status the card was in before the transition,
    /// or <see langword="null"/> for the initial assignment at card creation.
    /// </param>
    /// <param name="newStatusId">The status the card transitioned into.</param>
    /// <param name="changedBy">
    /// The actor identifier. Must not be <see langword="null"/> or whitespace.
    /// </param>
    /// <param name="changeSource">Whether the change was manual or triggered by automation.</param>
    /// <returns>A new, immutable <see cref="CardStatusHistory"/> record.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="changedBy"/> is <see langword="null"/> or whitespace.
    /// </exception>
    internal static CardStatusHistory Create(
        Guid cardId,
        Guid? previousStatusId,
        Guid newStatusId,
        string changedBy,
        ChangeSource changeSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);

        return new CardStatusHistory
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            PreviousStatusId = previousStatusId,
            NewStatusId = newStatusId,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = changedBy,
            ChangeSource = changeSource
        };
    }
}
