using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.AddBoardStatus;

/// <summary>
/// Command to add a new board status. When <see cref="ProjectId"/> is <see langword="null"/>,
/// the status is added as a global default; otherwise it is added to the specified project.
/// </summary>
public sealed record AddBoardStatusCommand
    : IRequest<AddBoardStatusCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The status name. Required, 1-100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The project to add the status to, or <see langword="null"/> for a global default.
    /// </summary>
    public Guid? ProjectId { get; init; }
}
