using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByTransition;

/// <summary>
/// Handles the <see cref="GetWorkflowTriggersByTransitionQuery"/> by returning all enabled
/// workflow triggers for the specified project that match the given status transition.
/// </summary>
public sealed class GetWorkflowTriggersByTransitionQueryHandler
    : IRequestHandler<GetWorkflowTriggersByTransitionQuery, ValueTask<List<WorkflowTriggerDto>>>
{
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ILogger<GetWorkflowTriggersByTransitionQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetWorkflowTriggersByTransitionQueryHandler"/> class.
    /// </summary>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetWorkflowTriggersByTransitionQueryHandler(
        IWorkflowTriggerRepository workflowTriggerRepository,
        ILogger<GetWorkflowTriggersByTransitionQueryHandler> logger)
    {
        _workflowTriggerRepository = workflowTriggerRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<WorkflowTriggerDto>> Handle(
        GetWorkflowTriggersByTransitionQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Fetching enabled triggers for transition {FromStatusId} → {ToStatusId} in project {ProjectId}",
            request.FromStatusId,
            request.ToStatusId,
            request.ProjectId);

        return await _workflowTriggerRepository.GetByTransitionAsync(
            request.ProjectId,
            request.FromStatusId,
            request.ToStatusId,
            cancellationToken);
    }
}
