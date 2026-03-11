using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowTriggerRepository"/>.
/// Provides data access operations for the <see cref="WorkflowTrigger"/> entity
/// and its associated <see cref="WorkflowTriggerAgent"/> records.
/// </summary>
public sealed class WorkflowTriggerRepository : IWorkflowTriggerRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WorkflowTriggerRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTriggerRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public WorkflowTriggerRepository(
        ApplicationDbContext dbContext,
        ILogger<WorkflowTriggerRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<WorkflowTriggerDto>> GetByProjectIdAsync(
        Guid projectId,
        bool? isEnabled = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching workflow triggers for project {ProjectId}", projectId);

        var query = _dbContext.WorkflowTriggers
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId);

        if (isEnabled.HasValue)
        {
            query = query.Where(t => t.IsEnabled == isEnabled.Value);
        }

        var triggers = await query
            .Include(t => t.TriggerAgents)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return triggers
            .Select(t => new WorkflowTriggerDto(
                t.Id,
                t.ProjectId,
                t.Name,
                t.FromStatusId,
                t.ToStatusId,
                t.Prompt,
                t.IsEnabled,
                t.CreatedAt,
                t.UpdatedAt,
                t.TriggerAgents
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new WorkflowTriggerAgentDto(a.Id, a.WorkflowTriggerId, a.AgentName, a.SortOrder))
                    .ToList()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<WorkflowTriggerDto>> GetByTransitionAsync(
        Guid projectId,
        Guid fromStatusId,
        Guid toStatusId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching enabled triggers for transition {FromStatusId} → {ToStatusId} in project {ProjectId}",
            fromStatusId, toStatusId, projectId);

        var triggers = await _dbContext.WorkflowTriggers
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId
                     && t.IsEnabled
                     && t.FromStatusId == fromStatusId
                     && t.ToStatusId == toStatusId)
            .Include(t => t.TriggerAgents)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return triggers
            .Select(t => new WorkflowTriggerDto(
                t.Id,
                t.ProjectId,
                t.Name,
                t.FromStatusId,
                t.ToStatusId,
                t.Prompt,
                t.IsEnabled,
                t.CreatedAt,
                t.UpdatedAt,
                t.TriggerAgents
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new WorkflowTriggerAgentDto(a.Id, a.WorkflowTriggerId, a.AgentName, a.SortOrder))
                    .ToList()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<WorkflowTrigger?> GetEntityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowTriggers
            .Include(t => t.TriggerAgents)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithStatusIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowTriggers
            .AnyAsync(t => t.FromStatusId == statusId || t.ToStatusId == statusId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(WorkflowTrigger trigger, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkflowTriggers.AddAsync(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(WorkflowTrigger trigger)
    {
        _dbContext.WorkflowTriggers.Remove(trigger);
    }

    /// <inheritdoc />
    public void RemoveAgents(IEnumerable<WorkflowTriggerAgent> agents)
    {
        _dbContext.WorkflowTriggerAgents.RemoveRange(agents);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
