using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.AddAgentComment;

/// <summary>
/// Handles the <see cref="AddAgentCommentCommand"/> by loading the target card, delegating
/// comment creation to the domain entity, and persisting the new <c>CardComment</c> record.
/// </summary>
public sealed class AddAgentCommentCommandHandler
    : IRequestHandler<AddAgentCommentCommand, ValueTask<Result<Guid>>>
{
    private readonly ICardRepository _cardRepository;
    private readonly ICardCommentRepository _cardCommentRepository;
    private readonly ILogger<AddAgentCommentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAgentCommentCommandHandler"/> class.
    /// </summary>
    /// <param name="cardRepository">The card repository.</param>
    /// <param name="cardCommentRepository">The card comment repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AddAgentCommentCommandHandler(
        ICardRepository cardRepository,
        ICardCommentRepository cardCommentRepository,
        ILogger<AddAgentCommentCommandHandler> logger)
    {
        _cardRepository = cardRepository;
        _cardCommentRepository = cardCommentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        AddAgentCommentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adding agent comment to card {CardId} by agent {AgentName} (trigger {WorkflowTriggerId})",
            request.CardId, request.AgentName, request.WorkflowTriggerId);

        var card = await _cardRepository.GetEntityByIdAsync(request.CardId, cancellationToken);

        if (card is null)
        {
            _logger.LogWarning(
                "Card {CardId} not found when adding agent comment by {AgentName}",
                request.CardId, request.AgentName);

            return Result<Guid>.Failure("Card not found.");
        }

        var comment = card.AddComment(
            request.Content,
            CommentAuthorType.Agent,
            agentName: request.AgentName,
            workflowTriggerId: request.WorkflowTriggerId);

        await _cardCommentRepository.AddAsync(comment, cancellationToken);
        await _cardCommentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Agent comment {CommentId} added to card {CardId} by agent {AgentName}",
            comment.Id, request.CardId, request.AgentName);

        return Result<Guid>.Success(comment.Id);
    }
}
