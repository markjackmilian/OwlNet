using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.CreateProject;

/// <summary>
/// Command to create a new project. Returns the created project's ID on success.
/// </summary>
public sealed record CreateProjectCommand : IRequest<CreateProjectCommand, ValueTask<Result<Guid>>>
{
    /// <summary>
    /// The project name. Required, between 3 and 100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The optional project description. Maximum 500 characters.
    /// </summary>
    public string? Description { get; init; }
}
