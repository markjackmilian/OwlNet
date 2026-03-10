namespace OwlNet.Application.Common.Models;

/// <summary>
/// Data transfer object representing a system agent.
/// </summary>
/// <param name="Id">The unique system agent identifier.</param>
/// <param name="Name">
/// The stable agent identifier used as the default filename (without <c>.md</c> extension)
/// when the agent is installed into a project. Immutable after creation.
/// </param>
/// <param name="DisplayName">The human-readable label shown in the UI.</param>
/// <param name="Description">The short summary of the agent's purpose.</param>
/// <param name="Mode">
/// The OpenCode agent mode. One of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
/// </param>
/// <param name="Content">
/// The full Markdown content of the agent file, including YAML frontmatter and body.
/// </param>
/// <param name="CreatedAt">When the system agent was first created.</param>
/// <param name="UpdatedAt">When the system agent was last modified.</param>
public sealed record SystemAgentDto(
    Guid Id,
    string Name,
    string DisplayName,
    string Description,
    string Mode,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
