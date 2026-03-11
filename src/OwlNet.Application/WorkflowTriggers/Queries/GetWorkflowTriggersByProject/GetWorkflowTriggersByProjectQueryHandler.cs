using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByProject;

/// <summary>
/// Handles the <see cref="GetWorkflowTriggersByProjectQuery"/> by returning all workflow
/// triggers for the specified project, optionally filtered by their enabled/disabled state.
/// </summary>
public sealed class GetWorkflowTriggersByProjectQueryHandler
    : IRequestHandler<GetWorkflowTriggersByProjectQuery, ValueTask<List<WorkflowTriggerDto>>>
{
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ILogger<GetWorkflowTriggersByProjectQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetWorkflowTriggersByProjectQueryHandler"/> class.
    /// </summary>
    /// <param name="workflowTriggerRepository">The workflow trigger repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetWorkflowTriggersByProjectQueryHandler(
        IWorkflowTriggerRepository workflowTriggerRepository,
        ILogger<GetWorkflowTriggersByProjectQueryHandler> logger)
    {
        _workflowTriggerRepository = workflowTriggerRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<List<WorkflowTriggerDto>> Handle(
        GetWorkflowTriggersByProjectQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Fetching workflow triggers for project {ProjectId}, isEnabled filter: {IsEnabled}",
            request.ProjectId,
            request.IsEnabled);

        return await _workflowTriggerRepository.GetByProjectIdAsync(
            request.ProjectId,
            request.IsEnabled,
            cancellationToken);
    }
}
