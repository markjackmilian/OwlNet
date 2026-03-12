using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardAttachments;

/// <summary>
/// Handles the <see cref="GetCardAttachmentsQuery"/> by returning all attachments for a specific
/// card as read-only projections (without <c>Content</c>), ordered by creation time ascending.
/// </summary>
public sealed class GetCardAttachmentsQueryHandler
    : IRequestHandler<GetCardAttachmentsQuery, ValueTask<IReadOnlyList<CardAttachmentDto>>>
{
    private readonly ICardAttachmentRepository _attachmentRepository;
    private readonly ILogger<GetCardAttachmentsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCardAttachmentsQueryHandler"/> class.
    /// </summary>
    /// <param name="attachmentRepository">The card attachment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetCardAttachmentsQueryHandler(
        ICardAttachmentRepository attachmentRepository,
        ILogger<GetCardAttachmentsQueryHandler> logger)
    {
        _attachmentRepository = attachmentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CardAttachmentDto>> Handle(
        GetCardAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving attachments for card {CardId}",
            request.CardId);

        var attachments = await _attachmentRepository.GetByCardIdAsync(
            request.CardId,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {AttachmentCount} attachment(s) for card {CardId}",
            attachments.Count, request.CardId);

        return attachments;
    }
}
