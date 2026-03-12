using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="CardAttachment"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
/// <remarks>
/// <see cref="CardAttachment"/> records are append-only — this interface intentionally exposes no
/// update or delete operations, enforcing the immutability guarantee defined in the domain.
/// </remarks>
public interface ICardAttachmentRepository
{
    /// <summary>
    /// Adds a new attachment to the data store.
    /// The attachment is not persisted until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="attachment">The <see cref="CardAttachment"/> entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(CardAttachment attachment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all attachments for the specified card as read-only projections (without <c>Content</c>),
    /// ordered by <see cref="CardAttachment.CreatedAt"/> ascending (oldest first).
    /// </summary>
    /// <remarks>
    /// The returned <see cref="CardAttachmentDto"/> includes a denormalised
    /// <see cref="CardAttachmentDto.WorkflowTriggerName"/> resolved at query time from the related
    /// <see cref="WorkflowTrigger"/>. This field is <see langword="null"/> when the attachment was
    /// not created via a trigger, or when the referenced trigger has since been deleted (set-null
    /// FK behaviour).
    /// <c>Content</c> is intentionally excluded from the projection — use
    /// <see cref="GetContentByIdAsync"/> to load the full content of a single attachment on demand.
    /// </remarks>
    /// <param name="cardId">The identifier of the card whose attachments to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="CardAttachmentDto"/> for the specified card, ordered by
    /// <see cref="CardAttachment.CreatedAt"/> ascending. Returns an empty list if the card has no
    /// attachments.
    /// </returns>
    ValueTask<List<CardAttachmentDto>> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full <c>Content</c> string of the specified attachment, for on-demand loading.
    /// </summary>
    /// <param name="attachmentId">The identifier of the attachment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The full Markdown content of the attachment if found; otherwise <see langword="null"/>.
    /// </returns>
    ValueTask<string?> GetContentByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}
