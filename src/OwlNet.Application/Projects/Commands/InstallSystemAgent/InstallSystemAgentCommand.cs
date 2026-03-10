using DispatchR.Abstractions.Send;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Projects.Commands.InstallSystemAgent;

/// <summary>
/// Command to install a system agent into a project by writing its content
/// as a Markdown file inside the project's <c>.opencode/agents/</c> directory.
/// Returns a non-generic <see cref="Result"/> because the installation does not
/// produce a new resource identifier — the target path is fully determined by
/// <see cref="ProjectId"/> and <see cref="FileName"/>.
/// </summary>
public sealed record InstallSystemAgentCommand : IRequest<InstallSystemAgentCommand, ValueTask<Result>>
{
    /// <summary>
    /// The ID of the system agent to install.
    /// </summary>
    public required Guid SystemAgentId { get; init; }

    /// <summary>
    /// The ID of the target project into which the agent file will be written.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// The name of the target file, without the <c>.md</c> extension.
    /// May differ from <c>SystemAgent.Name</c> when the user customises the filename
    /// before installing (e.g. to avoid a collision with an existing agent file).
    /// Must be 2–50 characters and contain only alphanumeric characters and hyphens.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Controls whether an existing file with the same name may be overwritten.
    /// Set to <c>false</c> on the first installation attempt; set to <c>true</c>
    /// only after the user has explicitly confirmed the overwrite in the UI (FR-60).
    /// </summary>
    public required bool AllowOverwrite { get; init; }
}
