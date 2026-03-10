using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetSystemAgentById;

/// <summary>
/// Handles the <see cref="GetSystemAgentByIdQuery"/> by returning a single system agent
/// matching the requested identifier.
/// </summary>
public sealed class GetSystemAgentByIdQueryHandler
    : IRequestHandler<GetSystemAgentByIdQuery, ValueTask<Result<SystemAgentDto>>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<GetSystemAgentByIdQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemAgentByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetSystemAgentByIdQueryHandler(
        ISystemAgentRepository repository,
        ILogger<GetSystemAgentByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<SystemAgentDto>> Handle(
        GetSystemAgentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var agent = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (agent is null)
        {
            _logger.LogWarning("System agent {SystemAgentId} not found", request.Id);
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

        return Result<SystemAgentDto>.Success(dto);
    }
}
