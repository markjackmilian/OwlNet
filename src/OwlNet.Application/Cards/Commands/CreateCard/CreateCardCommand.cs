using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;

namespace OwlNet.Application.Cards.Commands.CreateCard;

/// <summary>
/// Command to create a new card on the Kanban board.
/// When <see cref="StatusId"/> is provided the card is assigned to that status;
/// otherwise it falls back to the project's first status (lowest <c>SortOrder</c>).
/// Returns a <see cref="Result{T}"/> containing both the new card's <c>Id</c> and its
/// sequential <c>Number</c> within the project.
/// </summary>
public sealed record CreateCardCommand
    : IRequest<CreateCardCommand, ValueTask<Result<(Guid Id, int Number)>>>
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
    /// The identifier of the board status to assign the card to.
    /// When <see langword="null"/>, the handler assigns the status with the lowest <c>SortOrder</c>.
    /// </summary>
    public Guid? StatusId { get; init; }

    /// <summary>
    /// The identifier of the user creating the card (e.g. ASP.NET Identity user ID).
    /// </summary>
    public required string CreatedBy { get; init; }
}
