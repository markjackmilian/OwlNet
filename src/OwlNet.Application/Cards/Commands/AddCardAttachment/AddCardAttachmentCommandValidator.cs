using FluentValidation;

namespace OwlNet.Application.Cards.Commands.AddCardAttachment;

/// <summary>
/// FluentValidation validator for <see cref="AddCardAttachmentCommand"/>.
/// Enforces that the target card and workflow trigger are specified, the file name and agent name
/// are non-empty and within the 200-character limit, and that attachment content is provided.
/// </summary>
public sealed class AddCardAttachmentCommandValidator : AbstractValidator<AddCardAttachmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddCardAttachmentCommandValidator"/> class.
    /// </summary>
    public AddCardAttachmentCommandValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name cannot be empty.")
            .MaximumLength(200).WithMessage("File name cannot exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Attachment content cannot be empty.");

        RuleFor(x => x.AgentName)
            .NotEmpty().WithMessage("Agent name cannot be empty.")
            .MaximumLength(200).WithMessage("Agent name cannot exceed 200 characters.");

        RuleFor(x => x.WorkflowTriggerId)
            .NotEmpty();
    }
}
