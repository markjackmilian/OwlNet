using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardAttachmentContent;

/// <summary>
/// Query to retrieve the full Markdown content of a single card attachment, for on-demand loading.
/// Returns a <see cref="Result{T}"/> wrapping the content string, or a failure result if the
/// attachment is not found.
/// </summary>
public sealed record GetCardAttachmentContentQuery
    : IRequest<GetCardAttachmentContentQuery, ValueTask<Result<string>>>
{
    /// <summary>The identifier of the attachment whose content to retrieve.</summary>
    public required Guid AttachmentId { get; init; }
}
