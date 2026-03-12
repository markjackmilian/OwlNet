using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Cards.Commands.AddHumanComment;

/// <summary>
/// Command to add a human-authored comment to an existing card.
/// The comment is attributed to the authenticated user identified by <see cref="AuthorId"/>.
/// </summary>
public sealed record AddHumanCommentCommand
    : IRequest<AddHumanCommentCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The identifier of the card to comment on.
    /// </summary>
    public required Guid CardId { get; init; }

    /// <summary>
    /// The comment body. Supports Markdown. Must be between 1 and 10,000 characters.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The authenticated user's identifier (e.g. ASP.NET Identity user ID).
    /// </summary>
    public required string AuthorId { get; init; }
}
