using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="ProjectTag"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
/// <remarks>
/// Tag uniqueness within a project (case-insensitive) is enforced at the Application layer
/// via <see cref="ExistsByNameAsync"/> before any write operation.
/// Cascade deletion of <see cref="CardTag"/> join records when a tag is removed is handled
/// automatically by EF Core's configured cascade delete behaviour.
/// </remarks>
public interface IProjectTagRepository
{
    /// <summary>
    /// Returns the <see cref="ProjectTag"/> domain entity by its ID.
    /// The returned entity is tracked by EF Core and can be mutated and saved.
    /// Used by command handlers that need to call <see cref="ProjectTag.Rename"/> or
    /// <see cref="ProjectTag.UpdateColor"/>.
    /// </summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="ProjectTag"/> entity if found; otherwise <see langword="null"/>.
    /// </returns>
    ValueTask<ProjectTag?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all tags belonging to the specified project as read-only projections,
    /// ordered by <see cref="ProjectTag.Name"/> ascending.
    /// </summary>
    /// <param name="projectId">The identifier of the project whose tags to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="ProjectTagDto"/> for the specified project, ordered by
    /// <see cref="ProjectTag.Name"/> ascending. Returns an empty list if the project has no tags.
    /// </returns>
    ValueTask<IReadOnlyList<ProjectTagDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a tag with the given name already exists within the specified project.
    /// The comparison is case-insensitive.
    /// </summary>
    /// <param name="projectId">The project scope to check within.</param>
    /// <param name="name">The tag name to check.</param>
    /// <param name="excludeTagId">
    /// An optional tag ID to exclude from the check (used during renames so the tag's
    /// own current name does not trigger a duplicate).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if a tag with the name exists in the project;
    /// otherwise <see langword="false"/>.
    /// </returns>
    ValueTask<bool> ExistsByNameAsync(
        Guid projectId,
        string name,
        Guid? excludeTagId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tag to the data store.
    /// The tag is not persisted until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="tag">The <see cref="ProjectTag"/> entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask AddAsync(ProjectTag tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a tag for removal from the data store (hard delete).
    /// Associated <c>CardTag</c> join records are cascade-deleted by EF Core.
    /// The removal is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="tag">The <see cref="ProjectTag"/> entity to remove.</param>
    void Remove(ProjectTag tag);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}
