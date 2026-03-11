using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.ReorderBoardStatuses;

/// <summary>
/// Command to reorder board statuses within a scope (global defaults or a specific project).
/// The <see cref="OrderedStatusIds"/> list defines the new sort order — the first ID gets
/// sort order 0, the second gets 1, and so on.
/// </summary>
public sealed record ReorderBoardStatusesCommand
    : IRequest<ReorderBoardStatusesCommand, ValueTask<Result>>
{
    /// <summary>
    /// The project scope, or <see langword="null"/> for global defaults.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// The complete ordered list of board status IDs defining the new sort order.
    /// The index in the list becomes the new <see cref="Domain.Entities.BoardStatus.SortOrder"/> value.
    /// </summary>
    public required List<Guid> OrderedStatusIds { get; init; }
}
