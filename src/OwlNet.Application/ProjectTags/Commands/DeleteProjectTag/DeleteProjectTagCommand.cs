using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.ProjectTags.Commands.DeleteProjectTag;

/// <summary>
/// Command to permanently delete a project tag.
/// All <c>CardTag</c> join records referencing this tag are cascade-deleted by the database.
/// This operation is irreversible.
/// </summary>
public sealed record DeleteProjectTagCommand
    : IRequest<DeleteProjectTagCommand, ValueTask<Result>>
{
    /// <summary>
    /// The identifier of the tag to delete.
    /// </summary>
    public required Guid TagId { get; init; }
}
