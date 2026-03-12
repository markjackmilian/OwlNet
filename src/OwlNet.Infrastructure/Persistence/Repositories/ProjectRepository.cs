using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectRepository"/>.
/// Provides data access operations for the <see cref="Project"/> entity.
/// </summary>
public sealed class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProjectRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectRepository(
        ApplicationDbContext dbContext,
        ILogger<ProjectRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProjectDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Path,
                p.Description,
                p.IsArchived,
                p.IsFavorited,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Path,
                p.Description,
                p.IsArchived,
                p.IsFavorited,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Project?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        // Name uniqueness is enforced only among active (non-archived) projects.
        // Archived projects are excluded so that their names can be freely reused
        // by new or restored projects without triggering a false conflict.
        var query = _dbContext.Projects
            .Where(p => !p.IsArchived && p.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithPathAsync(
        string path,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        // Path uniqueness is enforced only among active (non-archived) projects.
        // Archived projects are excluded so that their filesystem path can be
        // reused by a new project without triggering a false conflict.
        // Comparison is case-insensitive because Windows filesystem paths are
        // case-insensitive (e.g. "C:\Code" and "c:\code" are the same directory).
        var query = _dbContext.Projects
            .Where(p => !p.IsArchived && p.Path.ToLower() == path.Trim().ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
