using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISystemAgentRepository"/>.
/// Provides data access operations for the <see cref="SystemAgent"/> entity.
/// </summary>
public sealed class SystemAgentRepository : ISystemAgentRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SystemAgentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemAgentRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public SystemAgentRepository(
        ApplicationDbContext dbContext,
        ILogger<SystemAgentRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemAgent>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SystemAgents
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SystemAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Tracking is intentional: the returned entity is mutated by Update/Delete handlers
        // before being passed back to UpdateAsync/DeleteAsync, which call SaveChangesAsync.
        return await _dbContext.SystemAgents
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SystemAgent?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _dbContext.SystemAgents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(SystemAgent agent, CancellationToken cancellationToken)
    {
        await _dbContext.SystemAgents.AddAsync(agent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("System agent {SystemAgentId} staged and persisted", agent.Id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SystemAgent agent, CancellationToken cancellationToken)
    {
        _dbContext.SystemAgents.Update(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("System agent {SystemAgentId} updated and persisted", agent.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(SystemAgent agent, CancellationToken cancellationToken)
    {
        _dbContext.SystemAgents.Remove(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("System agent {SystemAgentId} removed and persisted", agent.Id);
    }
}
