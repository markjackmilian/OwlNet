using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="Card"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
public interface ICardRepository
{
    /// <summary>
    /// Returns the <see cref="Card"/> domain entity by its ID.
    /// The returned entity is tracked by EF Core and can be mutated and saved.
    /// Used by command handlers that need to call <see cref="Card.Update"/> or
    /// <see cref="Card.ChangeStatus"/>.
    /// </summary>
    /// <param name="id">The card identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="Card"/> entity if found; otherwise <see langword="null"/>.
    /// </returns>
    ValueTask<Card?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all cards belonging to the specified project as read-only projections,
    /// with optional filtering by status and/or priority.
    /// Results are ordered by <see cref="Card.Number"/> ascending.
    /// </summary>
    /// <param name="projectId">The identifier of the project whose cards to retrieve.</param>
    /// <param name="statusId">
    /// When provided, only cards whose current <see cref="Card.StatusId"/> matches this value
    /// are returned. Pass <see langword="null"/> to include cards in any status.
    /// </param>
    /// <param name="priority">
    /// When provided, only cards with the specified <see cref="CardPriority"/> are returned.
    /// Pass <see langword="null"/> to include cards of any priority.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="CardDto"/> for the specified project, ordered by
    /// <see cref="Card.Number"/> ascending. Returns an empty list if no cards match.
    /// </returns>
    ValueTask<List<CardDto>> GetByProjectIdAsync(
        Guid projectId,
        Guid? statusId = null,
        CardPriority? priority = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full status transition history for the specified card,
    /// ordered by <see cref="CardStatusHistory.ChangedAt"/> descending (most recent first).
    /// </summary>
    /// <param name="cardId">The identifier of the card whose history to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="CardStatusHistoryDto"/> ordered by
    /// <see cref="CardStatusHistory.ChangedAt"/> descending.
    /// Returns an empty list if the card has no history records.
    /// </returns>
    ValueTask<List<CardStatusHistoryDto>> GetStatusHistoryAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next available sequential card number for the specified project.
    /// Computed as <c>MAX(Number) + 1</c> across all cards in the project,
    /// or <c>1</c> if the project has no cards yet.
    /// </summary>
    /// <remarks>
    /// The caller is responsible for assigning the returned number to the new card
    /// before persisting. This method does not reserve or lock the number.
    /// </remarks>
    /// <param name="projectId">The identifier of the project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next sequential number to assign to a new card in the project.</returns>
    ValueTask<int> GetNextNumberAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether at least one <see cref="Card"/> currently references the specified
    /// <see cref="BoardStatus"/>.
    /// Used to block deletion of a status that is still in use by one or more cards.
    /// </summary>
    /// <param name="statusId">The board status identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if one or more cards have <see cref="Card.StatusId"/> equal to
    /// <paramref name="statusId"/>; otherwise <see langword="false"/>.
    /// </returns>
    ValueTask<bool> ExistsWithStatusAsync(Guid statusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new card to the data store.
    /// The card is not persisted until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="card">The <see cref="Card"/> entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(Card card, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a card for removal from the data store (hard delete).
    /// Associated <see cref="CardStatusHistory"/> records are cascade-deleted by the database.
    /// The removal is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="card">The <see cref="Card"/> entity to remove.</param>
    void Remove(Card card);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}
