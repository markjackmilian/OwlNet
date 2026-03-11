using FluentValidation;

namespace OwlNet.Application.WorkflowTriggers.Commands.DeleteWorkflowTrigger;

/// <summary>
/// FluentValidation validator for <see cref="DeleteWorkflowTriggerCommand"/>.
/// Ensures the trigger identifier is provided before the handler executes.
/// </summary>
public sealed class DeleteWorkflowTriggerCommandValidator
    : AbstractValidator<DeleteWorkflowTriggerCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteWorkflowTriggerCommandValidator"/> class.
    /// </summary>
    public DeleteWorkflowTriggerCommandValidator()
    {
        RuleFor(x => x.TriggerId)
            .NotEmpty().WithMessage("Trigger ID is required.");
    }
}
