using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Queries.GetAllSystemAgents;

/// <summary>
/// Handles the <see cref="GetAllSystemAgentsQuery"/> by returning all system agents
/// ordered by name ascending.
/// </summary>
public sealed class GetAllSystemAgentsQueryHandler
    : IRequestHandler<GetAllSystemAgentsQuery, ValueTask<Result<IReadOnlyList<SystemAgentDto>>>>
{
    private readonly ISystemAgentRepository _repository;
    private readonly ILogger<GetAllSystemAgentsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllSystemAgentsQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The system agent repository.</param>
    /// <param name="logger">The logger instance.</param>
    public GetAllSystemAgentsQueryHandler(
        ISystemAgentRepository repository,
        ILogger<GetAllSystemAgentsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Result<IReadOnlyList<SystemAgentDto>>> Handle(
        GetAllSystemAgentsQuery request,
        CancellationToken cancellationToken)
    {
        var agents = await _repository.GetAllAsync(cancellationToken);

        var dtos = agents
            .Select(a => new SystemAgentDto(
                a.Id,
                a.Name,
                a.DisplayName,
                a.Description,
                a.Mode,
                a.Content,
                a.CreatedAt,
                a.UpdatedAt))
            .ToList()
            .AsReadOnly();

        _logger.LogDebug("Retrieved {SystemAgentCount} system agents", dtos.Count);

        return Result<IReadOnlyList<SystemAgentDto>>.Success(dtos);
    }
}
