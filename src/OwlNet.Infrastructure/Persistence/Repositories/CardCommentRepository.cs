using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICardCommentRepository"/>.
/// Provides append-only data access operations for <see cref="CardComment"/> entities.
/// </summary>
/// <remarks>
/// Read queries use <c>AsNoTracking()</c> for performance. The <see cref="GetByCardIdAsync"/>
/// projection resolves <see cref="CardCommentDto.WorkflowTriggerName"/> via a correlated
/// subquery rather than <c>Include()</c>, so that a deleted trigger (set-null FK) is handled
/// gracefully — the name simply returns <see langword="null"/>.
/// </remarks>
public sealed class CardCommentRepository : ICardCommentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CardCommentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardCommentRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public CardCommentRepository(
        ApplicationDbContext context,
        ILogger<CardCommentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(CardComment comment, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Adding comment {CommentId} to card {CardId} (authorType={AuthorType})",
            comment.Id, comment.CardId, comment.AuthorType);

        await _context.CardComments.AddAsync(comment, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<List<CardCommentDto>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching comments for card {CardId}", cardId);

        return await _context.CardComments
            .AsNoTracking()
            .Where(cc => cc.CardId == cardId)
            .OrderBy(cc => cc.CreatedAt)
            .Select(cc => new CardCommentDto(
                cc.Id,
                cc.CardId,
                cc.Content,
                cc.AuthorType,
                cc.AuthorId,
                cc.AgentName,
                cc.WorkflowTriggerId,
                cc.WorkflowTriggerId == null
                    ? null
                    : _context.WorkflowTriggers
                        .Where(wt => wt.Id == cc.WorkflowTriggerId)
                        .Select(wt => wt.Name)
                        .FirstOrDefault(),
                cc.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
