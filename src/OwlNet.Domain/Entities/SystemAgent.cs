using System.Text.RegularExpressions;

namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents an installation-wide AI agent definition that can be used as a template
/// across multiple projects. A system agent stores the full Markdown content (including
/// YAML frontmatter) that is copied into a project's <c>.opencode/agents/</c> directory
/// when installed.
/// </summary>
public sealed class SystemAgent
{
    private static readonly Regex NamePattern =
        new(@"^[a-zA-Z0-9-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    /// <summary>
    /// Gets the unique identifier for this system agent.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the stable agent identifier used as the default filename (without <c>.md</c> extension)
    /// when the agent is installed into a project. Immutable after creation.
    /// Must be 2–50 characters and contain only alphanumeric characters and hyphens (<c>[a-zA-Z0-9-]</c>).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the human-readable label shown in the UI.
    /// Must be 2–100 characters.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the short summary of the agent's purpose.
    /// Must be 10–500 characters.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the OpenCode agent mode. Must be one of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </summary>
    public string Mode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the full Markdown content of the agent file, including YAML frontmatter and body.
    /// Must not be empty or whitespace.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp indicating when this system agent was first created.
    /// Never updated after creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this system agent was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private SystemAgent() { }

    /// <summary>
    /// Creates a new <see cref="SystemAgent"/> with the specified properties.
    /// </summary>
    /// <param name="name">
    /// The stable agent identifier. Must be 2–50 characters and match <c>^[a-zA-Z0-9-]+$</c>.
    /// Immutable after creation.
    /// </param>
    /// <param name="displayName">
    /// The human-readable label. Must be 2–100 characters.
    /// </param>
    /// <param name="description">
    /// The short summary of the agent's purpose. Must be 10–500 characters.
    /// </param>
    /// <param name="mode">
    /// The OpenCode agent mode. Must be one of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </param>
    /// <param name="content">
    /// The full Markdown content of the agent file. Must not be empty or whitespace.
    /// </param>
    /// <returns>A new <see cref="SystemAgent"/> instance with <see cref="CreatedAt"/> and <see cref="UpdatedAt"/> set to <see cref="DateTimeOffset.UtcNow"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter fails its validation rules.
    /// </exception>
    public static SystemAgent Create(string name, string displayName, string description, string mode, string content)
    {
        ValidateName(name);
        ValidateDisplayName(displayName);
        ValidateDescription(description);
        ValidateMode(mode);
        ValidateContent(content);

        var now = DateTimeOffset.UtcNow;

        return new SystemAgent
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = description,
            Mode = mode,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the mutable properties of this system agent and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// <see cref="Name"/> is intentionally excluded — it is immutable after creation.
    /// </summary>
    /// <param name="displayName">
    /// The new human-readable label. Must be 2–100 characters.
    /// </param>
    /// <param name="description">
    /// The new short summary. Must be 10–500 characters.
    /// </param>
    /// <param name="mode">
    /// The new OpenCode agent mode. Must be one of <c>"primary"</c>, <c>"subagent"</c>, or <c>"all"</c>.
    /// </param>
    /// <param name="content">
    /// The new full Markdown content. Must not be empty or whitespace.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any parameter fails its validation rules.
    /// </exception>
    public void Update(string displayName, string description, string mode, string content)
    {
        ValidateDisplayName(displayName);
        ValidateDescription(description);
        ValidateMode(mode);
        ValidateContent(content);

        DisplayName = displayName;
        Description = description;
        Mode = mode;
        Content = content;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("System agent name must not be null or whitespace.", nameof(name));
        }

        if (name.Length < 2 || name.Length > 50)
        {
            throw new ArgumentException("System agent name must be between 2 and 50 characters.", nameof(name));
        }

        if (!NamePattern.IsMatch(name))
        {
            throw new ArgumentException("System agent name must contain only alphanumeric characters and hyphens.", nameof(name));
        }
    }

    private static void ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("System agent display name must not be null or whitespace.", nameof(displayName));
        }

        if (displayName.Length < 2 || displayName.Length > 100)
        {
            throw new ArgumentException("System agent display name must be between 2 and 100 characters.", nameof(displayName));
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("System agent description must not be null or whitespace.", nameof(description));
        }

        if (description.Length < 10 || description.Length > 500)
        {
            throw new ArgumentException("System agent description must be between 10 and 500 characters.", nameof(description));
        }
    }

    private static void ValidateMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new ArgumentException("System agent mode must not be null or whitespace.", nameof(mode));
        }

        if (mode is not ("primary" or "subagent" or "all"))
        {
            throw new ArgumentException("System agent mode must be one of: \"primary\", \"subagent\", \"all\".", nameof(mode));
        }
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("System agent content must not be null, empty, or whitespace.", nameof(content));
        }
    }
}
