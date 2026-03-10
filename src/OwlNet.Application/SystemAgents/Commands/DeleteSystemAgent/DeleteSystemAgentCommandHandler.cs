using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.DeleteSystemAgent;

/// <summary>
/// Handles the <see cref="DeleteSystemAgentCommand"/> by permanently removing a system agent
/// from the data store.
/// </summary>
public sealed class DeleteSystemAgentCommandHandler
    : IRequestHandler<DeleteSystemAgentCommand, ValueTask<Result>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<DeleteSystemAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSystemAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteSystemAgentCommandHandler(
        ISystemAgentRepository repository,
        ILogger<DeleteSystemAgentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result> Handle(
        DeleteSystemAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting system agent {SystemAgentId}", request.Id);

        var agent = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (agent is null)
        {
            _logger.LogWarning("System agent {SystemAgentId} not found", request.Id);
            return Result.Failure("System agent not found.");
        }

        await _repository.DeleteAsync(agent, cancellationToken);

        _logger.LogInformation("System agent {SystemAgentId} deleted", agent.Id);

        return Result.Success();
    }
}
