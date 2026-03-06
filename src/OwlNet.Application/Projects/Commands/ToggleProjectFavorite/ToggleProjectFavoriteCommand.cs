using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.ToggleProjectFavorite;

/// <summary>
/// Command to toggle the favorite status of an existing project.
/// </summary>
public sealed record ToggleProjectFavoriteCommand : IRequest<ToggleProjectFavoriteCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project to toggle favorite status for.
    /// </summary>
    public required Guid Id { get; init; }
}
