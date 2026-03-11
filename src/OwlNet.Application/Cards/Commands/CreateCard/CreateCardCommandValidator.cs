using FluentValidation;

namespace OwlNet.Application.Cards.Commands.CreateCard;

/// <summary>
/// FluentValidation validator for <see cref="CreateCardCommand"/>.
/// Enforces title length, optional description length, and creator identity constraints.
/// </summary>
public sealed class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCardCommandValidator"/> class.
    /// </summary>
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Card title is required.")
            .MaximumLength(200).WithMessage("Card title must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Card description must be 5000 characters or fewer.")
            .When(x => x.Description is not null);

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
