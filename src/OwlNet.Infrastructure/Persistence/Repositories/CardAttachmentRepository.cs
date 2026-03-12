using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICardAttachmentRepository"/>.
/// Provides append-only data access operations for <see cref="CardAttachment"/> entities.
/// </summary>
/// <remarks>
/// Read queries use <c>AsNoTracking()</c> for performance. The <see cref="GetByCardIdAsync"/>
/// projection resolves <see cref="CardAttachmentDto.WorkflowTriggerName"/> via a correlated
/// subquery rather than <c>Include()</c>, so that a deleted trigger (set-null FK) is handled
/// gracefully — the name simply returns <see langword="null"/>.
/// <see cref="CardAttachment.Content"/> is intentionally excluded from the list projection;
/// use <see cref="GetContentByIdAsync"/> to load the full content of a single attachment on demand.
/// </remarks>
public sealed class CardAttachmentRepository : ICardAttachmentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CardAttachmentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardAttachmentRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    public CardAttachmentRepository(
        ApplicationDbContext context,
        ILogger<CardAttachmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(CardAttachment attachment, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Adding attachment {AttachmentId} to card {CardId} by agent {AgentName}",
            attachment.Id, attachment.CardId, attachment.AgentName);

        await _context.CardAttachments.AddAsync(attachment, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<List<CardAttachmentDto>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching attachments for card {CardId}", cardId);

        return await _context.CardAttachments
            .AsNoTracking()
            .Where(ca => ca.CardId == cardId)
            .OrderBy(ca => ca.CreatedAt)
            .Select(ca => new CardAttachmentDto(
                ca.Id,
                ca.CardId,
                ca.FileName,
                ca.AgentName,
                ca.WorkflowTriggerId,
                ca.WorkflowTriggerId == null
                    ? null
                    : _context.WorkflowTriggers
                        .Where(wt => wt.Id == ca.WorkflowTriggerId)
                        .Select(wt => wt.Name)
                        .FirstOrDefault(),
                ca.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetContentByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching content for attachment {AttachmentId}", attachmentId);

        return await _context.CardAttachments
            .AsNoTracking()
            .Where(ca => ca.Id == attachmentId)
            .Select(ca => ca.Content)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
