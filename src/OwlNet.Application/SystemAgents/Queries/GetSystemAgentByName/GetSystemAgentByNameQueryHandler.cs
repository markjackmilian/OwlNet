using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetSystemAgentByName;

/// <summary>
/// Handles the <see cref="GetSystemAgentByNameQuery"/> by returning a single system agent
/// matching the requested name.
/// </summary>
public sealed class GetSystemAgentByNameQueryHandler
    : IRequestHandler<GetSystemAgentByNameQuery, ValueTask<Result<SystemAgentDto>>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<GetSystemAgentByNameQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemAgentByNameQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetSystemAgentByNameQueryHandler(
        ISystemAgentRepository repository,
        ILogger<GetSystemAgentByNameQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<SystemAgentDto>> Handle(
        GetSystemAgentByNameQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving system agent by name {SystemAgentName}", request.Name);

        var agent = await _repository.GetByNameAsync(request.Name, cancellationToken);

        if (agent is null)
        {
            _logger.LogWarning("System agent {SystemAgentName} not found", request.Name);
            return Result<SystemAgentDto>.Failure("System agent not found.");
        }

        var dto = new SystemAgentDto(
            agent.Id,
            agent.Name,
            agent.DisplayName,
            agent.Description,
            agent.Mode,
            agent.Content,
            agent.CreatedAt,
            agent.UpdatedAt);

        _logger.LogDebug("System agent {SystemAgentName} retrieved successfully", request.Name);

        return Result<SystemAgentDto>.Success(dto);
    }
}
