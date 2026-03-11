using FluentValidation;

namespace OwlNet.Application.Cards.Commands.DeleteCard;

/// <summary>
/// FluentValidation validator for <see cref="DeleteCardCommand"/>.
/// Enforces that the card identifier is provided.
/// </summary>
public sealed class DeleteCardCommandValidator : AbstractValidator<DeleteCardCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCardCommandValidator"/> class.
    /// </summary>
    public DeleteCardCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Card ID is required.");
    }
}
