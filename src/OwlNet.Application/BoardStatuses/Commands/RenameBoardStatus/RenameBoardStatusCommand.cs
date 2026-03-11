using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.BoardStatuses.Commands.RenameBoardStatus;

/// <summary>
/// Command to rename an existing board status.
/// Supports both project-level statuses (FR-7) and global defaults (FR-10).
/// </summary>
public sealed record RenameBoardStatusCommand
    : IRequest<RenameBoardStatusCommand, ValueTask<Result>>
{
    /// <summary>
    /// The board status identifier to rename.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The new name for the status. Required, 1-100 characters.
    /// </summary>
    public required string NewName { get; init; }
}
