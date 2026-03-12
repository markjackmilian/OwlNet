using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="CardComment"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
/// <remarks>
/// <see cref="CardComment"/> records are append-only — this interface intentionally exposes no
/// update or delete operations, enforcing the immutability guarantee defined in the domain.
/// </remarks>
public interface ICardCommentRepository
{
    /// <summary>
    /// Adds a new comment to the data store.
    /// The comment is not persisted until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="comment">The <see cref="CardComment"/> entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(CardComment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all comments for the specified card as read-only projections,
    /// ordered by <see cref="CardComment.CreatedAt"/> ascending (oldest first, chronological thread).
    /// </summary>
    /// <remarks>
    /// The returned <see cref="CardCommentDto"/> includes a denormalised
    /// <see cref="CardCommentDto.WorkflowTriggerName"/> resolved at query time from the related
    /// <see cref="WorkflowTrigger"/>. This field is <see langword="null"/> when the comment was
    /// not posted via a trigger, or when the referenced trigger has since been deleted (set-null
    /// FK behaviour).
    /// </remarks>
    /// <param name="cardId">The identifier of the card whose comments to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="CardCommentDto"/> for the specified card, ordered by
    /// <see cref="CardComment.CreatedAt"/> ascending. Returns an empty list if the card has no
    /// comments.
    /// </returns>
    ValueTask<List<CardCommentDto>> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}
