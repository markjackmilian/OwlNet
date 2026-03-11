using FluentValidation;

namespace OwlNet.Application.BoardStatuses.Commands.DeleteBoardStatus;

/// <summary>
/// FluentValidation validator for <see cref="DeleteBoardStatusCommand"/>.
/// Enforces identifier constraints.
/// </summary>
public sealed class DeleteBoardStatusCommandValidator : AbstractValidator<DeleteBoardStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteBoardStatusCommandValidator"/> class.
    /// </summary>
    public DeleteBoardStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Board status ID is required.");
    }
}
