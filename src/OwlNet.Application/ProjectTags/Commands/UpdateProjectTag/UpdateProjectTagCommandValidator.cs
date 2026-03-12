using FluentValidation;

namespace OwlNet.Application.ProjectTags.Commands.UpdateProjectTag;

/// <summary>
/// FluentValidation validator for <see cref="UpdateProjectTagCommand"/>.
/// Enforces tag identifier presence and optional name/color format constraints.
/// </summary>
public sealed class UpdateProjectTagCommandValidator : AbstractValidator<UpdateProjectTagCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProjectTagCommandValidator"/> class.
    /// </summary>
    public UpdateProjectTagCommandValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name must not be empty when provided.")
            .MaximumLength(50).WithMessage("Tag name must be 50 characters or fewer.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid hex color code (e.g., #FF5733).")
            .When(x => x.Color is not null);
    }
}
