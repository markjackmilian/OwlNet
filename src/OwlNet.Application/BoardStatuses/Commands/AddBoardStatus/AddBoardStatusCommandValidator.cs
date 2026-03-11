using FluentValidation;

namespace OwlNet.Application.BoardStatuses.Commands.AddBoardStatus;

/// <summary>
/// FluentValidation validator for <see cref="AddBoardStatusCommand"/>.
/// Enforces name constraints: required and maximum 100 characters.
/// </summary>
public sealed class AddBoardStatusCommandValidator : AbstractValidator<AddBoardStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddBoardStatusCommandValidator"/> class.
    /// </summary>
    public AddBoardStatusCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Status name is required.")
            .MaximumLength(100).WithMessage("Status name must be 100 characters or fewer.");
    }
}
