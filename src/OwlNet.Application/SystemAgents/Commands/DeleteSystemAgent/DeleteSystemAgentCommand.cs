using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.SystemAgents.Commands.DeleteSystemAgent;

/// <summary>
/// Command to permanently delete a system agent by its unique identifier.
/// </summary>
public sealed record DeleteSystemAgentCommand : IRequest<DeleteSystemAgentCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the system agent to delete.
    /// </summary>
    public required Guid Id { get; init; }
}
