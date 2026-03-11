using FluentValidation;

namespace OwlNet.Application.Cards.Commands.UpdateCard;

/// <summary>
/// FluentValidation validator for <see cref="UpdateCardCommand"/>.
/// Enforces card identifier presence and title/description length constraints.
/// </summary>
public sealed class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCardCommandValidator"/> class.
    /// </summary>
    public UpdateCardCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Card ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Card title is required.")
            .MaximumLength(200).WithMessage("Card title must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Card description must be 5000 characters or fewer.")
            .When(x => x.Description is not null);
    }
}
