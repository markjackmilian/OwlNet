using FluentValidation;

namespace OwlNet.Application.Cards.Commands.AddAgentComment;

/// <summary>
/// FluentValidation validator for <see cref="AddAgentCommentCommand"/>.
/// Enforces that the target card is specified, the comment body is non-empty and within the
/// 10,000-character limit, and that an agent name is provided.
/// </summary>
public sealed class AddAgentCommentCommandValidator : AbstractValidator<AddAgentCommentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddAgentCommentCommandValidator"/> class.
    /// </summary>
    public AddAgentCommentCommandValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment cannot be empty.")
            .MaximumLength(10_000).WithMessage("Comment cannot exceed 10,000 characters.");

        RuleFor(x => x.AgentName)
            .NotEmpty().WithMessage("Agent name is required for agent comments.");
    }
}
