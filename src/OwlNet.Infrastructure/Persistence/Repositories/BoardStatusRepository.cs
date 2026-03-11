using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBoardStatusRepository"/>.
/// Provides data access operations for the <see cref="BoardStatus"/> entity.
/// </summary>
public sealed class BoardStatusRepository : IBoardStatusRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BoardStatusRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardStatusRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public BoardStatusRepository(
        ApplicationDbContext dbContext,
        ILogger<BoardStatusRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<BoardStatusDto>> GetGlobalDefaultsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all global default board statuses");

        return await _dbContext.BoardStatuses
            .AsNoTracking()
            .Where(b => b.ProjectId == null)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BoardStatusDto(
                b.Id,
                b.Name,
                b.SortOrder,
                b.IsDefault,
                b.ProjectId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<BoardStatusDto>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching board statuses for project {ProjectId}", projectId);

        return await _dbContext.BoardStatuses
            .AsNoTracking()
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BoardStatusDto(
                b.Id,
                b.Name,
                b.SortOrder,
                b.IsDefault,
                b.ProjectId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BoardStatus?> GetEntityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BoardStatuses
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<BoardStatus>> GetEntitiesByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BoardStatuses
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<BoardStatus>> GetGlobalDefaultEntitiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BoardStatuses
            .Where(b => b.ProjectId == null)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithNameInScopeAsync(
        string name,
        Guid? projectId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        // Name uniqueness is scoped: global defaults (ProjectId == null) form one scope,
        // and each project forms its own scope. The comparison is case-insensitive to
        // prevent confusing near-duplicates like "In Progress" vs "in progress".
        var query = _dbContext.BoardStatuses
            .Where(b => b.ProjectId == projectId)
            .Where(b => b.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BoardStatus status, CancellationToken cancellationToken = default)
    {
        await _dbContext.BoardStatuses.AddAsync(status, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<BoardStatus> statuses,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.BoardStatuses.AddRangeAsync(statuses, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(BoardStatus status)
    {
        _dbContext.BoardStatuses.Remove(status);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
