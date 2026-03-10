namespace OwlNet.Application.Common.Models;

/// <summary>
/// Data transfer object representing an agent file discovered on the filesystem.
/// </summary>
/// <param name="FileName">The filename without the <c>.md</c> extension, used as the agent name/identifier.</param>
/// <param name="FilePath">The full absolute path to the <c>.md</c> file.</param>
/// <param name="Mode">The value of the <c>mode</c> frontmatter field, or an empty string if absent.</param>
/// <param name="Description">The value of the <c>description</c> frontmatter field, or an empty string if absent.</param>
/// <param name="RawContent">The full raw file content including frontmatter.</param>
public sealed record AgentFileDto(
    string FileName,
    string FilePath,
    string Mode,
    string Description,
    string RawContent);
