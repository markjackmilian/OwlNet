using FluentValidation;

namespace OwlNet.Application.BoardStatuses.Commands.RenameBoardStatus;

/// <summary>
/// FluentValidation validator for <see cref="RenameBoardStatusCommand"/>.
/// Enforces identifier and name constraints.
/// </summary>
public sealed class RenameBoardStatusCommandValidator : AbstractValidator<RenameBoardStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenameBoardStatusCommandValidator"/> class.
    /// </summary>
    public RenameBoardStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Board status ID is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("Status name is required.")
            .MaximumLength(100).WithMessage("Status name must be 100 characters or fewer.");
    }
}
