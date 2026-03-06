using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.ArchiveProject;

/// <summary>
/// Command to archive an existing project (soft delete).
/// </summary>
public sealed record ArchiveProjectCommand : IRequest<ArchiveProjectCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project to archive.
    /// </summary>
    public required Guid Id { get; init; }
}
