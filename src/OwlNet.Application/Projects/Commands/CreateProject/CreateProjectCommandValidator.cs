using FluentValidation;

namespace OwlNet.Application.Projects.Commands.CreateProject;

/// <summary>
/// FluentValidation validator for <see cref="CreateProjectCommand"/>.
/// Enforces name length (3-100 chars) and description length (max 500 chars) constraints.
/// </summary>
public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectCommandValidator"/> class.
    /// </summary>
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MinimumLength(3).WithMessage("Project name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Project name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Project description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
