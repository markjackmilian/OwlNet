using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.UpdateCard;

/// <summary>
/// Command to update the title, description, and priority of an existing card.
/// Status changes must be performed via <c>ChangeCardStatusCommand</c>.
/// </summary>
public sealed record UpdateCardCommand
    : IRequest<UpdateCardCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the card to update.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The new card title. Required, 1–200 characters.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The new description supporting Markdown. Maximum 5000 characters.
    /// Pass <see langword="null"/> to clear the description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The new priority level of the card.
    /// </summary>
    public required CardPriority Priority { get; init; }
}
