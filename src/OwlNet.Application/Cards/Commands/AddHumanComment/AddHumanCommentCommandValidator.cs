using FluentValidation;

namespace OwlNet.Application.Cards.Commands.AddHumanComment;

/// <summary>
/// FluentValidation validator for <see cref="AddHumanCommentCommand"/>.
/// Enforces that the target card is specified, the comment body is non-empty and within the
/// 10,000-character limit, and that a human author identifier is provided.
/// </summary>
public sealed class AddHumanCommentCommandValidator : AbstractValidator<AddHumanCommentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddHumanCommentCommandValidator"/> class.
    /// </summary>
    public AddHumanCommentCommandValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment cannot be empty.")
            .MaximumLength(10_000).WithMessage("Comment cannot exceed 10,000 characters.");

        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("Author ID is required for human comments.");
    }
}
