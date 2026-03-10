using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Provides filesystem-based operations for agent definition files stored in a
/// project's <c>.opencode/agents/</c> directory. Each agent is a Markdown file
/// with optional YAML frontmatter containing <c>mode</c> and <c>description</c> fields.
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IAgentFileService
{
    /// <summary>
    /// Discovers and returns all agent definition files in the project's
    /// <c>.opencode/agents/</c> directory.
    /// </summary>
    /// <param name="projectPath">The absolute filesystem path to the project root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only list of <see cref="AgentFileDto"/> representing every <c>.md</c> file
    /// found in the agents directory. Returns an empty list if the directory does not exist
    /// or contains no agent files.
    /// </returns>
    Task<IReadOnlyList<AgentFileDto>> GetAgentsAsync(string projectPath, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a single agent definition file by name.
    /// </summary>
    /// <param name="projectPath">The absolute filesystem path to the project root.</param>
    /// <param name="agentName">
    /// The agent name (filename without the <c>.md</c> extension) to look up.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AgentFileDto"/> if a matching <c>.md</c> file exists in the project's
    /// <c>.opencode/agents/</c> directory; otherwise <see langword="null"/>.
    /// </returns>
    Task<AgentFileDto?> GetAgentAsync(string projectPath, string agentName, CancellationToken cancellationToken);

    /// <summary>
    /// Writes or overwrites an agent definition file in the project's
    /// <c>.opencode/agents/</c> directory. Creates the directory structure if it
    /// does not already exist.
    /// </summary>
    /// <param name="projectPath">The absolute filesystem path to the project root.</param>
    /// <param name="agentName">
    /// The agent name used as the filename (without the <c>.md</c> extension).
    /// </param>
    /// <param name="content">
    /// The full file content to write, including any YAML frontmatter and Markdown body.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAgentAsync(string projectPath, string agentName, string content, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an agent definition file from the project's <c>.opencode/agents/</c> directory.
    /// </summary>
    /// <param name="projectPath">The absolute filesystem path to the project root.</param>
    /// <param name="agentName">
    /// The agent name (filename without the <c>.md</c> extension) to delete.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAgentAsync(string projectPath, string agentName, CancellationToken cancellationToken);
}
