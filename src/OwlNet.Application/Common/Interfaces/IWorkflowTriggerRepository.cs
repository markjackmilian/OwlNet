using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Repository interface for <see cref="WorkflowTrigger"/> entity persistence operations.
/// Implemented in the Infrastructure layer with EF Core.
/// </summary>
public interface IWorkflowTriggerRepository
{
    /// <summary>
    /// Returns all triggers belonging to the specified project as read-only projections,
    /// with their associated <see cref="WorkflowTriggerAgent"/> entries included,
    /// ordered by <see cref="WorkflowTrigger.CreatedAt"/> ascending.
    /// </summary>
    /// <param name="projectId">The identifier of the project whose triggers to retrieve.</param>
    /// <param name="isEnabled">
    /// When provided, only triggers whose <see cref="WorkflowTrigger.IsEnabled"/> matches this
    /// value are returned. Pass <see langword="null"/> to include both enabled and disabled
    /// triggers.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="WorkflowTriggerDto"/> for the specified project, ordered by
    /// <see cref="WorkflowTrigger.CreatedAt"/> ascending. Returns an empty list if no triggers
    /// match.
    /// </returns>
    Task<List<WorkflowTriggerDto>> GetByProjectIdAsync(
        Guid projectId,
        bool? isEnabled = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all <em>enabled</em> triggers for the specified project that match the given
    /// status transition (<paramref name="fromStatusId"/> → <paramref name="toStatusId"/>),
    /// ordered by <see cref="WorkflowTrigger.CreatedAt"/> ascending so that triggers are
    /// evaluated in creation order (oldest first).
    /// Used by the trigger evaluation engine when a card's status changes.
    /// </summary>
    /// <param name="projectId">The identifier of the project to search within.</param>
    /// <param name="fromStatusId">
    /// The identifier of the source <see cref="BoardStatus"/> that the card is transitioning
    /// away from.
    /// </param>
    /// <param name="toStatusId">
    /// The identifier of the destination <see cref="BoardStatus"/> that the card is
    /// transitioning into.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="WorkflowTriggerDto"/> whose <see cref="WorkflowTrigger.IsEnabled"/>
    /// is <see langword="true"/> and whose <see cref="WorkflowTrigger.FromStatusId"/> and
    /// <see cref="WorkflowTrigger.ToStatusId"/> match the supplied values, ordered by
    /// <see cref="WorkflowTrigger.CreatedAt"/> ascending. Returns an empty list if no matching
    /// triggers are found.
    /// </returns>
    Task<List<WorkflowTriggerDto>> GetByTransitionAsync(
        Guid projectId,
        Guid fromStatusId,
        Guid toStatusId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="WorkflowTrigger"/> domain entity by its ID, with its
    /// <see cref="WorkflowTrigger.TriggerAgents"/> collection eagerly loaded.
    /// The returned entity is tracked by EF Core and can be mutated and saved.
    /// Used by command handlers that need to call <see cref="WorkflowTrigger.Update"/>,
    /// <see cref="WorkflowTrigger.SetAgents"/>, <see cref="WorkflowTrigger.Enable"/>, or
    /// <see cref="WorkflowTrigger.Disable"/>.
    /// </summary>
    /// <param name="id">The workflow trigger identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The tracked <see cref="WorkflowTrigger"/> entity with its agents loaded if found;
    /// otherwise <see langword="null"/>.
    /// </returns>
    Task<WorkflowTrigger?> GetEntityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether at least one <see cref="WorkflowTrigger"/> references the specified
    /// <see cref="BoardStatus"/> as either its <see cref="WorkflowTrigger.FromStatusId"/> or
    /// its <see cref="WorkflowTrigger.ToStatusId"/>.
    /// Used as a guard in <c>DeleteBoardStatusCommandHandler</c> to warn or block deletion of a
    /// status that is still referenced by one or more workflow triggers.
    /// </summary>
    /// <param name="statusId">The board status identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if at least one trigger has <see cref="WorkflowTrigger.FromStatusId"/>
    /// or <see cref="WorkflowTrigger.ToStatusId"/> equal to <paramref name="statusId"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    Task<bool> ExistsWithStatusIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new workflow trigger to the data store.
    /// The trigger is not persisted until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/> entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(WorkflowTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a workflow trigger for removal from the data store (hard delete).
    /// Associated <see cref="WorkflowTriggerAgent"/> records are cascade-deleted by the
    /// database. The removal is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/> entity to remove.</param>
    void Remove(WorkflowTrigger trigger);

    /// <summary>
    /// Marks the specified <see cref="WorkflowTriggerAgent"/> records for removal from the
    /// data store. Used by the update handler to clear the existing agent list before replacing
    /// it with a new one. The removal is persisted when <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="agents">The agent records to remove.</param>
    void RemoveAgents(IEnumerable<WorkflowTriggerAgent> agents);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
