using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Filesystem-based implementation of <see cref="IAgentFileService"/> that reads and writes
/// agent definition Markdown files from a project's <c>.opencode/agents/</c> directory.
/// Each agent file may contain optional YAML frontmatter with <c>mode</c> and <c>description</c> fields.
/// </summary>
public sealed class AgentFileService : IAgentFileService
{
    private const string AgentsRelativePath = ".opencode/agents";
    private const string MarkdownExtension = ".md";
    private const string MarkdownSearchPattern = "*.md";
    private const string FrontmatterDelimiter = "---";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AgentFileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileService"/> class.
    /// </summary>
    /// <param name="fileSystem">The filesystem abstraction for all I/O operations.</param>
    /// <param name="logger">The logger instance for structured diagnostics.</param>
    public AgentFileService(IFileSystem fileSystem, ILogger<AgentFileService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentFileDto>> GetAgentsAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        var agentsDir = GetAgentsDirectory(projectPath);

        if (!_fileSystem.DirectoryExists(agentsDir))
        {
            _logger.LogDebug("Agents directory does not exist at {AgentsDirectory}, returning empty list", agentsDir);
            return [];
        }

        try
        {
            var filePaths = _fileSystem.GetFiles(agentsDir, MarkdownSearchPattern);
            var agents = await ReadAgentFilesAsync(filePaths, cancellationToken);

            _logger.LogInformation(
                "Discovered {AgentCount} agent files in {AgentsDirectory}",
                agents.Count,
                agentsDir);

            return agents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate agent files in {AgentsDirectory}", agentsDir);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<AgentFileDto?> GetAgentAsync(
        string projectPath,
        string agentName,
        CancellationToken cancellationToken)
    {
        var agentsDir = GetAgentsDirectory(projectPath);
        var filePath = Path.Combine(agentsDir, agentName + MarkdownExtension);

        if (!_fileSystem.FileExists(filePath))
        {
            _logger.LogDebug("Agent file not found at {FilePath}", filePath);
            return null;
        }

        try
        {
            var content = await _fileSystem.ReadAllTextAsync(filePath, cancellationToken);
            var (mode, description) = ParseFrontmatter(content);

            return new AgentFileDto(
                FileName: Path.GetFileNameWithoutExtension(filePath),
                FilePath: filePath,
                Mode: mode,
                Description: description,
                RawContent: content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read agent file at {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteAgentAsync(
        string projectPath,
        string agentName,
        string content,
        CancellationToken cancellationToken)
    {
        var agentsDir = GetAgentsDirectory(projectPath);
        var filePath = Path.Combine(agentsDir, agentName + MarkdownExtension);

        try
        {
            if (!_fileSystem.DirectoryExists(agentsDir))
            {
                _fileSystem.CreateDirectory(agentsDir);
                _logger.LogInformation("Created agents directory at {AgentsDirectory}", agentsDir);
            }

            await _fileSystem.WriteAllTextAsync(filePath, content, cancellationToken);
            _logger.LogInformation("Wrote agent file {AgentName} at {FilePath}", agentName, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write agent file {AgentName} at {FilePath}", agentName, filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public Task DeleteAgentAsync(
        string projectPath,
        string agentName,
        CancellationToken cancellationToken)
    {
        var agentsDir = GetAgentsDirectory(projectPath);
        var filePath = Path.Combine(agentsDir, agentName + MarkdownExtension);

        try
        {
            if (!_fileSystem.FileExists(filePath))
            {
                _logger.LogDebug("Agent file {AgentName} does not exist at {FilePath}, nothing to delete", agentName, filePath);
                return Task.CompletedTask;
            }

            _fileSystem.DeleteFile(filePath);
            _logger.LogInformation("Deleted agent file {AgentName} at {FilePath}", agentName, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete agent file {AgentName} at {FilePath}", agentName, filePath);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Constructs the absolute path to the agents directory for the given project.
    /// </summary>
    private static string GetAgentsDirectory(string projectPath) =>
        Path.Combine(projectPath, AgentsRelativePath);

    /// <summary>
    /// Reads multiple agent files in parallel, skipping any that fail to read.
    /// Returns the results sorted alphabetically by filename.
    /// </summary>
    private async Task<IReadOnlyList<AgentFileDto>> ReadAgentFilesAsync(
        string[] filePaths,
        CancellationToken cancellationToken)
    {
        var agents = new List<AgentFileDto>(filePaths.Length);

        foreach (var filePath in filePaths)
        {
            var agent = await TryReadAgentFileAsync(filePath, cancellationToken);

            if (agent is not null)
            {
                agents.Add(agent);
            }
        }

        agents.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));
        return agents;
    }

    /// <summary>
    /// Attempts to read and parse a single agent file. Returns <see langword="null"/> on failure.
    /// </summary>
    private async Task<AgentFileDto?> TryReadAgentFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _fileSystem.ReadAllTextAsync(filePath, cancellationToken);
            var (mode, description) = ParseFrontmatter(content);

            return new AgentFileDto(
                FileName: Path.GetFileNameWithoutExtension(filePath),
                FilePath: filePath,
                Mode: mode,
                Description: description,
                RawContent: content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skipping agent file that could not be read at {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Parses optional YAML frontmatter delimited by <c>---</c> lines at the start of the content.
    /// Extracts <c>mode</c> and <c>description</c> key-value pairs using simple line-by-line parsing.
    /// Returns empty strings if frontmatter is missing, malformed, or does not contain the expected keys.
    /// </summary>
    private static (string Mode, string Description) ParseFrontmatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (string.Empty, string.Empty);
        }

        try
        {
            var lines = content.Split('\n');

            if (lines.Length == 0 || lines[0].Trim() != FrontmatterDelimiter)
            {
                return (string.Empty, string.Empty);
            }

            var mode = string.Empty;
            var description = string.Empty;

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line == FrontmatterDelimiter)
                {
                    break;
                }

                ExtractFrontmatterValue(line, ref mode, ref description);
            }

            return (mode, description);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Checks a single frontmatter line for <c>mode:</c> or <c>description:</c> keys
    /// and assigns the trimmed value to the corresponding output parameter.
    /// </summary>
    private static void ExtractFrontmatterValue(string line, ref string mode, ref string description)
    {
        var colonIndex = line.IndexOf(':');

        if (colonIndex < 0)
        {
            return;
        }

        var key = line[..colonIndex].Trim();
        var value = line[(colonIndex + 1)..].Trim();

        if (key.Equals("mode", StringComparison.OrdinalIgnoreCase))
        {
            mode = value;
        }
        else if (key.Equals("description", StringComparison.OrdinalIgnoreCase))
        {
            description = value;
        }
    }
}
