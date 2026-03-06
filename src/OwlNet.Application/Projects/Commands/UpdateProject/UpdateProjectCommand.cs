using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.UpdateProject;

/// <summary>
/// Command to update an existing project's name and/or description.
/// </summary>
public sealed record UpdateProjectCommand : IRequest<UpdateProjectCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the project to update.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The new project name. Required, between 3 and 100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The new optional project description. Maximum 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
