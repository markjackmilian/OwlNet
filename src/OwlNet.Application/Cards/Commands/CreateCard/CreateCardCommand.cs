using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.CreateCard;

/// <summary>
/// Command to create a new card on the Kanban board.
/// The card is assigned to the project's first status (lowest <c>SortOrder</c>).
/// </summary>
public sealed record CreateCardCommand
    : IRequest<CreateCardCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The card title. Required, 1–200 characters.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// An optional description supporting Markdown. Maximum 5000 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The priority level of the new card.
    /// </summary>
    public required CardPriority Priority { get; init; }

    /// <summary>
    /// The identifier of the project this card belongs to.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The identifier of the user creating the card (e.g. ASP.NET Identity user ID).
    /// </summary>
    public required string CreatedBy { get; init; }
}
