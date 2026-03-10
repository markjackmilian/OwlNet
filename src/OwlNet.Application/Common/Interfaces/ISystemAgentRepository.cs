using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="SystemAgent"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
public interface ISystemAgentRepository
{
    /// <summary>
    /// Returns all system agents sorted alphabetically by <see cref="SystemAgent.Name"/> ascending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of all <see cref="SystemAgent"/> entities.</returns>
    Task<IReadOnlyList<SystemAgent>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns a single <see cref="SystemAgent"/> by its unique identifier.
    /// </summary>
    /// <param name="id">The system agent identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="SystemAgent"/> entity if found; otherwise <see langword="null"/>.</returns>
    Task<SystemAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a single <see cref="SystemAgent"/> by its stable name identifier.
    /// Used to enforce the unique-name constraint before creation.
    /// </summary>
    /// <param name="name">The agent name to look up (case-sensitive, matches the unique index).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="SystemAgent"/> entity if found; otherwise <see langword="null"/>.</returns>
    Task<SystemAgent?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new <see cref="SystemAgent"/> to the data store and persists the change immediately.
    /// This method is self-contained: it stages the entity and calls <c>SaveChangesAsync</c> internally.
    /// </summary>
    /// <param name="agent">The system agent entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(SystemAgent agent, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an existing <see cref="SystemAgent"/> as modified and persists all pending changes.
    /// </summary>
    /// <param name="agent">The system agent entity with updated property values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(SystemAgent agent, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a <see cref="SystemAgent"/> from the data store and persists the deletion.
    /// </summary>
    /// <param name="agent">The system agent entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(SystemAgent agent, CancellationToken cancellationToken);
}
