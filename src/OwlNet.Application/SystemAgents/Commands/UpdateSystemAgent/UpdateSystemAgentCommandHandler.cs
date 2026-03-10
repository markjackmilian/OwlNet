using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.UpdateSystemAgent;

/// <summary>
/// Handles the <see cref="UpdateSystemAgentCommand"/> by updating an existing system agent's
/// mutable properties and persisting the changes.
/// </summary>
public sealed class UpdateSystemAgentCommandHandler
    : IRequestHandler<UpdateSystemAgentCommand, ValueTask<Result>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<UpdateSystemAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSystemAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public UpdateSystemAgentCommandHandler(
        ISystemAgentRepository repository,
        ILogger<UpdateSystemAgentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        UpdateSystemAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating system agent {SystemAgentId}", request.Id);

        var agent = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (agent is null)
        {
            _logger.LogWarning("System agent {SystemAgentId} not found", request.Id);
            return Result.Failure("System agent not found.");
        }

        agent.Update(request.DisplayName, request.Description, request.Mode, request.Content);

        await _repository.UpdateAsync(agent, cancellationToken);

        _logger.LogInformation("System agent {SystemAgentId} updated", agent.Id);

        return Result.Success();
    }
}
