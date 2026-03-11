using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="BoardStatus"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
public interface IBoardStatusRepository
{
    /// <summary>
    /// Returns all global default statuses (where <see cref="BoardStatus.ProjectId"/> is
    /// <see langword="null"/>), ordered by <see cref="BoardStatus.SortOrder"/> ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="BoardStatusDto"/> representing global default statuses.</returns>
    Task<List<BoardStatusDto>> GetGlobalDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all statuses for a specific project, ordered by
    /// <see cref="BoardStatus.SortOrder"/> ascending.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="BoardStatusDto"/> for the specified project.</returns>
    Task<List<BoardStatusDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="BoardStatus"/> domain entity by its ID.
    /// Used by command handlers that need to mutate the entity.
    /// </summary>
    /// <param name="id">The board status identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="BoardStatus"/> entity if found; otherwise <see langword="null"/>.</returns>
    Task<BoardStatus?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all <see cref="BoardStatus"/> domain entities for a specific project,
    /// ordered by <see cref="BoardStatus.SortOrder"/> ascending.
    /// Used by command handlers that need to reorder statuses within a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tracked <see cref="BoardStatus"/> entities for the specified project.</returns>
    Task<List<BoardStatus>> GetEntitiesByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all global default <see cref="BoardStatus"/> domain entities (where
    /// <see cref="BoardStatus.ProjectId"/> is <see langword="null"/>), ordered by
    /// <see cref="BoardStatus.SortOrder"/> ascending.
    /// Used by command handlers that need to reorder global default statuses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of tracked global default <see cref="BoardStatus"/> entities.</returns>
    Task<List<BoardStatus>> GetGlobalDefaultEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a board status with the given name already exists within the same scope
    /// (global defaults or a specific project). The comparison is case-insensitive.
    /// </summary>
    /// <param name="name">The status name to check.</param>
    /// <param name="projectId">
    /// The project scope to check within, or <see langword="null"/> to check global defaults.
    /// </param>
    /// <param name="excludeId">
    /// An optional board status ID to exclude from the check (used during renames
    /// so the current status's own name does not trigger a duplicate).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if a status with the name exists in the scope; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsWithNameInScopeAsync(
        string name,
        Guid? projectId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new board status to the data store.
    /// </summary>
    /// <param name="status">The board status entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(BoardStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple board statuses to the data store.
    /// Used when seeding a new project with default statuses.
    /// </summary>
    /// <param name="statuses">The board status entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<BoardStatus> statuses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a board status for removal from the data store.
    /// The removal is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="status">The board status entity to remove.</param>
    void Remove(BoardStatus status);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
