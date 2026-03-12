using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICardRepository"/>.
/// Provides data access operations for the <see cref="Card"/> entity and its
/// associated <see cref="CardStatusHistory"/> records.
/// </summary>
public sealed class CardRepository : ICardRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CardRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public CardRepository(
        ApplicationDbContext context,
        ILogger<CardRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Card?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching card entity {CardId}", id);

        return await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Card?> GetEntityByIdWithTagsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching card entity {CardId} with tags", id);

        return await _context.Cards
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<List<CardDto>> GetByProjectIdAsync(
        Guid projectId,
        Guid? statusId = null,
        CardPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching cards for project {ProjectId} (statusId={StatusId}, priority={Priority})",
            projectId, statusId, priority);

        return await _context.Cards
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .Where(c => statusId == null || c.StatusId == statusId)
            .Where(c => priority == null || c.Priority == priority)
            .OrderBy(c => c.Number)
            .Select(c => new CardDto(
                c.Id,
                c.Number,
                c.Title,
                c.Description,
                c.Priority,
                c.StatusId,
                _context.BoardStatuses
                    .Where(s => s.Id == c.StatusId)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? string.Empty,
                c.ProjectId,
                c.CreatedAt,
                c.UpdatedAt,
                c.Tags
                    .Select(ct => new ProjectTagDto(
                        ct.Tag.Id,
                        ct.Tag.ProjectId,
                        ct.Tag.Name,
                        ct.Tag.Color,
                        ct.Tag.CreatedAt,
                        ct.Tag.UpdatedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<List<CardStatusHistoryDto>> GetStatusHistoryAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching status history for card {CardId}", cardId);

        return await _context.CardStatusHistories
            .AsNoTracking()
            .Where(h => h.CardId == cardId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new CardStatusHistoryDto(
                h.Id,
                h.CardId,
                h.PreviousStatusId,
                h.PreviousStatusId == null
                    ? null
                    : _context.BoardStatuses
                        .Where(s => s.Id == h.PreviousStatusId)
                        .Select(s => s.Name)
                        .FirstOrDefault(),
                h.NewStatusId,
                _context.BoardStatuses
                    .Where(s => s.Id == h.NewStatusId)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? string.Empty,
                h.ChangedAt,
                h.ChangedBy,
                h.ChangeSource))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<int> GetNextNumberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Computing next card number for project {ProjectId}", projectId);

        var maxNumber = await _context.Cards
            .Where(c => c.ProjectId == projectId)
            .MaxAsync(c => (int?)c.Number, cancellationToken);

        return (maxNumber ?? 0) + 1;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsWithStatusAsync(Guid statusId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking whether any card references status {StatusId}", statusId);

        return await _context.Cards
            .AnyAsync(c => c.StatusId == statusId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding card {CardId} (project {ProjectId})", card.Id, card.ProjectId);

        await _context.Cards.AddAsync(card, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(Card card)
    {
        _logger.LogDebug("Removing card {CardId}", card.Id);

        _context.Cards.Remove(card);
    }

    /// <inheritdoc />
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
