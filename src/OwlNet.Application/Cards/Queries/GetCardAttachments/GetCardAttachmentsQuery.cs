using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardAttachments;

/// <summary>
/// Query to retrieve all attachments for a specific card, ordered by creation time ascending.
/// The returned <see cref="CardAttachmentDto"/> records do not include <c>Content</c> — use
/// <see cref="OwlNet.Application.Cards.Queries.GetCardAttachmentContent.GetCardAttachmentContentQuery"/>
/// to load the full content of a single attachment on demand.
/// </summary>
public sealed record GetCardAttachmentsQuery
    : IRequest<GetCardAttachmentsQuery, ValueTask<IReadOnlyList<CardAttachmentDto>>>
{
    /// <summary>The identifier of the card whose attachments to retrieve.</summary>
    public required Guid CardId { get; init; }
}
