using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;

namespace OwlNet.Application.SystemAgents.Commands.CreateSystemAgent;

/// <summary>
/// Handles the <see cref="CreateSystemAgentCommand"/> by creating a new system agent entity
/// and persisting it to the data store.
/// </summary>
public sealed class CreateSystemAgentCommandHandler
    : IRequestHandler<CreateSystemAgentCommand, ValueTask<Result<Guid>>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<CreateSystemAgentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSystemAgentCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateSystemAgentCommandHandler(
        ISystemAgentRepository repository,
        ILogger<CreateSystemAgentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Guid>> Handle(
        CreateSystemAgentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating system agent with name {SystemAgentName}", request.Name);

        var existing = await _repository.GetByNameAsync(request.Name, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure("A system agent with this name already exists.");
        }

        var agent = SystemAgent.Create(
            request.Name,
            request.DisplayName,
            request.Description,
            request.Mode,
            request.Content);

        await _repository.AddAsync(agent, cancellationToken);

        _logger.LogInformation(
            "System agent {SystemAgentId} created with name {SystemAgentName}",
            agent.Id,
            agent.Name);

        return Result<Guid>.Success(agent.Id);
    }
}
