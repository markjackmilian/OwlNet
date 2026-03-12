using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Queries.GetCardAttachmentContent;

/// <summary>
/// Handles the <see cref="GetCardAttachmentContentQuery"/> by loading the full Markdown content
/// of a single attachment on demand. Returns a failure result when the attachment is not found.
/// </summary>
public sealed class GetCardAttachmentContentQueryHandler
    : IRequestHandler<GetCardAttachmentContentQuery, ValueTask<Result<string>>>
{
    private readonly ICardAttachmentRepository _attachmentRepository;
    private readonly ILogger<GetCardAttachmentContentQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCardAttachmentContentQueryHandler"/> class.
    /// </summary>
    /// <param name="attachmentRepository">The card attachment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetCardAttachmentContentQueryHandler(
        ICardAttachmentRepository attachmentRepository,
        ILogger<GetCardAttachmentContentQueryHandler> logger)
    {
        _attachmentRepository = attachmentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<string>> Handle(
        GetCardAttachmentContentQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving content for attachment {AttachmentId}",
            request.AttachmentId);

        var content = await _attachmentRepository.GetContentByIdAsync(
            request.AttachmentId,
            cancellationToken);

        if (content is null)
        {
            _logger.LogWarning(
                "Attachment {AttachmentId} not found",
                request.AttachmentId);

            return Result<string>.Failure("Attachment not found.");
        }

        _logger.LogInformation(
            "Content retrieved for attachment {AttachmentId} ({ContentLength} chars)",
            request.AttachmentId, content.Length);

        return Result<string>.Success(content);
    }
}
