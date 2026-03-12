using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectTagRepository"/>.
/// Provides data access operations for the <see cref="ProjectTag"/> entity.
/// </summary>
public sealed class ProjectTagRepository : IProjectTagRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProjectTagRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectTagRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectTagRepository(
        ApplicationDbContext dbContext,
        ILogger<ProjectTagRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ProjectTag?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching project tag {TagId}", tagId);

        return await _dbContext.ProjectTags
            .FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ProjectTagDto>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all tags for project {ProjectId}", projectId);

        return await _dbContext.ProjectTags
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Name)
            .Select(t => new ProjectTagDto(
                t.Id,
                t.ProjectId,
                t.Name,
                t.Color,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsByNameAsync(
        Guid projectId,
        string name,
        Guid? excludeTagId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProjectTags
            .Where(t => t.ProjectId == projectId)
            .Where(t => t.Name.ToLower() == name.ToLower());

        if (excludeTagId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTagId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(ProjectTag tag, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProjectTags.AddAsync(tag, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(ProjectTag tag)
    {
        _dbContext.ProjectTags.Remove(tag);
    }

    /// <inheritdoc />
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
