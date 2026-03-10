using FluentValidation;

namespace OwlNet.Application.Projects.Commands.InstallSystemAgent;

/// <summary>
/// FluentValidation validator for <see cref="InstallSystemAgentCommand"/>.
/// Enforces that both IDs are non-empty and that the target filename satisfies
/// the format constraints (2–50 chars, alphanumeric characters and hyphens only).
/// </summary>
public sealed class InstallSystemAgentCommandValidator : AbstractValidator<InstallSystemAgentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstallSystemAgentCommandValidator"/> class.
    /// </summary>
    public InstallSystemAgentCommandValidator()
    {
        RuleFor(x => x.SystemAgentId)
            .NotEmpty().WithMessage("System agent ID is required.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MinimumLength(2).WithMessage("File name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("File name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9-]+$").WithMessage("File name can only contain letters, numbers, and hyphens.");
    }
}
