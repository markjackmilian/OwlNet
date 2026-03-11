using FluentValidation;

namespace OwlNet.Application.Cards.Commands.ChangeCardStatus;

/// <summary>
/// FluentValidation validator for <see cref="ChangeCardStatusCommand"/>.
/// Enforces that card ID, target status ID, and actor identity are all provided.
/// </summary>
public sealed class ChangeCardStatusCommandValidator : AbstractValidator<ChangeCardStatusCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCardStatusCommandValidator"/> class.
    /// </summary>
    public ChangeCardStatusCommandValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty().WithMessage("Card ID is required.");

        RuleFor(x => x.NewStatusId)
            .NotEmpty().WithMessage("New status ID is required.");

        RuleFor(x => x.ChangedBy)
            .NotEmpty().WithMessage("ChangedBy is required.");
    }
}
