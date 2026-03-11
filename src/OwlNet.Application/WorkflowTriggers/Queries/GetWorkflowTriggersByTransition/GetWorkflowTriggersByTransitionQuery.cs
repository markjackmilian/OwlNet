using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByTransition;

/// <summary>
/// Query to retrieve all enabled workflow triggers for a specific project that match
/// a given status transition (<see cref="FromStatusId"/> → <see cref="ToStatusId"/>).
/// Used by the trigger evaluation engine when a card's status changes.
/// </summary>
public sealed record GetWorkflowTriggersByTransitionQuery
    : IRequest<GetWorkflowTriggersByTransitionQuery, ValueTask<List<WorkflowTriggerDto>>>
{
    /// <summary>
    /// The identifier of the project to search within.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The identifier of the source board status that the card is transitioning away from.
    /// </summary>
    public required Guid FromStatusId { get; init; }

    /// <summary>
    /// The identifier of the destination board status that the card is transitioning into.
    /// </summary>
    public required Guid ToStatusId { get; init; }
}
