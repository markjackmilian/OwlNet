using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.RestoreProject;

/// <summary>
/// Command to restore an archived project, making it active again.
/// </summary>
public sealed record RestoreProjectCommand : IRequest<RestoreProjectCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project to restore.
    /// </summary>
    public required Guid Id { get; init; }
}
