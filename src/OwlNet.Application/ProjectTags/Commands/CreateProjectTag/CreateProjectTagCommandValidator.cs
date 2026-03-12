using FluentValidation;

namespace OwlNet.Application.ProjectTags.Commands.CreateProjectTag;

/// <summary>
/// FluentValidation validator for <see cref="CreateProjectTagCommand"/>.
/// Enforces name length, optional hex color format, and project identifier presence.
/// </summary>
public sealed class CreateProjectTagCommandValidator : AbstractValidator<CreateProjectTagCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectTagCommandValidator"/> class.
    /// </summary>
    public CreateProjectTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50).WithMessage("Tag name must be 50 characters or fewer.");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid hex color code (e.g., #FF5733).")
            .When(x => x.Color is not null);

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");
    }
}
