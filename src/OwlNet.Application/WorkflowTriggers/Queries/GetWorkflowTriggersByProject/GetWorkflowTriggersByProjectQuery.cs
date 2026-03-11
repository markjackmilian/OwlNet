using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByProject;

/// <summary>
/// Query to retrieve all workflow triggers belonging to a specific project,
/// with an optional filter on the enabled/disabled state.
/// </summary>
public sealed record GetWorkflowTriggersByProjectQuery
    : IRequest<GetWorkflowTriggersByProjectQuery, ValueTask<List<WorkflowTriggerDto>>>
{
    /// <summary>
    /// The identifier of the project whose workflow triggers to retrieve.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// When provided, only triggers whose enabled state matches this value are returned.
    /// Pass <see langword="null"/> to include both enabled and disabled triggers.
    /// </summary>
    public bool? IsEnabled { get; init; }
}
