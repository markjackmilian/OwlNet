using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="Project"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Returns all non-archived projects ordered by name ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="ProjectDto"/> representing active projects.</returns>
    Task<List<ProjectDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single project by its ID regardless of archived status.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ProjectDto"/> if found; otherwise <see langword="null"/>.</returns>
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="Project"/> domain entity by its ID.
    /// Used by command handlers that need to mutate the entity.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="Project"/> entity if found; otherwise <see langword="null"/>.</returns>
    Task<Project?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a project with the given name already exists (case-insensitive).
    /// </summary>
    /// <param name="name">The project name to check.</param>
    /// <param name="excludeId">
    /// An optional project ID to exclude from the check (used during updates
    /// so the current project's own name does not trigger a duplicate).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if a project with the name exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a project with the given filesystem path already exists.
    /// The check includes all projects (active and archived) to prevent path conflicts.
    /// </summary>
    /// <param name="path">The filesystem path to check.</param>
    /// <param name="excludeId">
    /// An optional project ID to exclude from the check (for future use during updates).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if a project with the path exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsWithPathAsync(string path, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new project to the data store.
    /// </summary>
    /// <param name="project">The project entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
