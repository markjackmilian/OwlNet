using FluentValidation;

namespace OwlNet.Application.SystemAgents.Commands.UpdateSystemAgent;

/// <summary>
/// FluentValidation validator for <see cref="UpdateSystemAgentCommand"/>.
/// Enforces a non-empty ID, display name length (2-100 chars), description length (10-500 chars),
/// valid mode values, and non-empty content constraints.
/// </summary>
public sealed class UpdateSystemAgentCommandValidator : AbstractValidator<UpdateSystemAgentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSystemAgentCommandValidator"/> class.
    /// </summary>
    public UpdateSystemAgentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("System agent ID is required.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Mode is required.")
            .Must(m => m is "primary" or "subagent" or "all")
            .WithMessage("Mode must be one of: primary, subagent, all.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Content must not be empty or whitespace.");
    }
}
