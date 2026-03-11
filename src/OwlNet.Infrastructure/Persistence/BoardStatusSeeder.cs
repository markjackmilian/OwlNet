using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence;

/// <summary>
/// Seeds the global default board statuses on first application run.
/// Idempotent — does nothing if global defaults already exist.
/// </summary>
public sealed class BoardStatusSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BoardStatusSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardStatusSeeder"/> class.
    /// </summary>
    /// <param name="dbContext">The application database context used to persist board statuses.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public BoardStatusSeeder(ApplicationDbContext dbContext, ILogger<BoardStatusSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the five global default board statuses (Backlog, ToEvaluate, Develop, Review, Done)
    /// if no global defaults exist yet. Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var globalDefaultsExist = await _dbContext.BoardStatuses
            .AnyAsync(b => b.ProjectId == null, cancellationToken);

        if (globalDefaultsExist)
        {
            _logger.LogDebug("Global default board statuses already exist, skipping seed");
            return;
        }

        BoardStatus[] defaults =
        [
            BoardStatus.CreateGlobalDefault("Backlog", sortOrder: 0),
            BoardStatus.CreateGlobalDefault("ToEvaluate", sortOrder: 1),
            BoardStatus.CreateGlobalDefault("Develop", sortOrder: 2),
            BoardStatus.CreateGlobalDefault("Review", sortOrder: 3),
            BoardStatus.CreateGlobalDefault("Done", sortOrder: 4),
        ];

        _dbContext.BoardStatuses.AddRange(defaults);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} global default board statuses", defaults.Length);
    }
}
