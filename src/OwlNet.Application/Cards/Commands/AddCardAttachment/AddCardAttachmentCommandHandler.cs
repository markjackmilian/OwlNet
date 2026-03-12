using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddCardAttachment;

/// <summary>
/// Handles the <see cref="AddCardAttachmentCommand"/> by loading the target card and workflow
/// trigger, delegating attachment creation to the domain entity, and persisting the new
/// <c>CardAttachment</c> record.
/// </summary>
public sealed class AddCardAttachmentCommandHandler
    : IRequestHandler<AddCardAttachmentCommand, ValueTask<Result<Guid>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ICardAttachmentRepository _attachmentRepository;
    private readonly ILogger<AddCardAttachmentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddCardAttachmentCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="attachmentRepository">The card attachment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AddCardAttachmentCommandHandler(
        ICardRepository cardRepository,
        IWorkflowTriggerRepository workflowTriggerRepository,
        ICardAttachmentRepository attachmentRepository,
        ILogger<AddCardAttachmentCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _workflowTriggerRepository = workflowTriggerRepository;
        _attachmentRepository = attachmentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        AddCardAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding attachment to card {CardId} by agent {AgentName} (trigger {WorkflowTriggerId})",
            request.CardId, request.AgentName, request.WorkflowTriggerId);

        var card = await _cardRepository.GetEntityByIdAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning(
                "Card {CardId} not found when adding attachment by {AgentName}",
                request.CardId, request.AgentName);

            return Result<Guid>.Failure("Card not found.");
        }

        var trigger = await _workflowTriggerRepository.GetEntityByIdAsync(
            request.WorkflowTriggerId,
            cancellationToken);

        if (trigger is null)
        {
            _logger.LogWarning(
                "WorkflowTrigger {WorkflowTriggerId} not found when adding attachment to card {CardId}",
                request.WorkflowTriggerId, request.CardId);

            return Result<Guid>.Failure("Workflow trigger not found.");
        }

        var attachment = card.AddAttachment(
            request.FileName,
            request.Content,
            request.AgentName,
            request.WorkflowTriggerId);

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _attachmentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Attachment {AttachmentId} added to card {CardId} by agent {AgentName}",
            attachment.Id, request.CardId, request.AgentName);

        return Result<Guid>.Success(attachment.Id);
    }
}
