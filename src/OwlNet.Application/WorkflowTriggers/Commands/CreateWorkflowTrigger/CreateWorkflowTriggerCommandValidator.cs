using FluentValidation;

namespace OwlNet.Application.WorkflowTriggers.Commands.CreateWorkflowTrigger;

/// <summary>
/// FluentValidation validator for <see cref="CreateWorkflowTriggerCommand"/>.
/// Enforces all field constraints and cross-field rules before the handler executes.
/// </summary>
public sealed class CreateWorkflowTriggerCommandValidator
    : AbstractValidator<CreateWorkflowTriggerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateWorkflowTriggerCommandValidator"/> class.
    /// </summary>
    public CreateWorkflowTriggerCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Trigger name is required.")
            .MaximumLength(150).WithMessage("Trigger name must be 150 characters or fewer.");

        RuleFor(x => x.FromStatusId)
            .NotEmpty().WithMessage("Source status ID is required.");

        RuleFor(x => x.ToStatusId)
            .NotEmpty().WithMessage("Destination status ID is required.");

        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Prompt is required.")
            .MaximumLength(10_000).WithMessage("Prompt must be 10 000 characters or fewer.");

        RuleFor(x => x.Agents)
            .NotEmpty().WithMessage("At least one agent is required.");

        RuleForEach(x => x.Agents).ChildRules(agent =>
        {
            agent.RuleFor(a => a.AgentName)
                .NotEmpty().WithMessage("Agent name is required.")
                .MaximumLength(200).WithMessage("Agent name must be 200 characters or fewer.");

            agent.RuleFor(a => a.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be greater than or equal to 0.");
        });

        RuleFor(x => x)
            .Must(x => x.FromStatusId != x.ToStatusId)
            .WithMessage("Source and destination status must be different.")
            .When(x => x.FromStatusId != Guid.Empty && x.ToStatusId != Guid.Empty);
    }
}
