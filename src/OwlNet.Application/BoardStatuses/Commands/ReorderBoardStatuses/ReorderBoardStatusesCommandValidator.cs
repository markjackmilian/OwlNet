using FluentValidation;

namespace OwlNet.Application.BoardStatuses.Commands.ReorderBoardStatuses;

/// <summary>
/// FluentValidation validator for <see cref="ReorderBoardStatusesCommand"/>.
/// Enforces that the ordered status ID list is present and contains no duplicates.
/// </summary>
public sealed class ReorderBoardStatusesCommandValidator : AbstractValidator<ReorderBoardStatusesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderBoardStatusesCommandValidator"/> class.
    /// </summary>
    public ReorderBoardStatusesCommandValidator()
    {
        RuleFor(x => x.OrderedStatusIds)
            .NotEmpty().WithMessage("Status order list is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate status IDs are not allowed.");
    }
}
