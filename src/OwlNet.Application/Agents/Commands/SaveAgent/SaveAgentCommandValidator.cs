using FluentValidation;

namespace OwlNet.Application.Agents.Commands.SaveAgent;

/// <summary>
/// FluentValidation validator for <see cref="SaveAgentCommand"/>.
/// Enforces project ID presence, agent name format (2-50 chars, alphanumeric + hyphens),
/// and non-empty content constraints.
/// </summary>
public sealed class SaveAgentCommandValidator : AbstractValidator<SaveAgentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveAgentCommandValidator"/> class.
    /// </summary>
    public SaveAgentCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.AgentName)
            .NotEmpty().WithMessage("Agent name is required.")
            .MinimumLength(2).WithMessage("Agent name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Agent name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$")
            .WithMessage("Agent name can only contain letters, numbers, and hyphens, and must start and end with a letter or number.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Agent content cannot be empty.");
    }
}
