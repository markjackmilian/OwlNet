namespace OwlNet.Domain.Entities;

/// <summary>
/// Represents a workflow trigger that fires when a card transitions between two specific
/// <see cref="BoardStatus"/> entries within a project.
/// When the trigger fires, each associated <see cref="WorkflowTriggerAgent"/> is invoked
/// sequentially (in ascending <see cref="WorkflowTriggerAgent.SortOrder"/>) with the
/// shared <see cref="Prompt"/> as context.
/// </summary>
public sealed class WorkflowTrigger
{
    private readonly List<WorkflowTriggerAgent> _triggerAgents = [];

    /// <summary>
    /// Gets the unique identifier for this workflow trigger.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="Project"/> this trigger belongs to.
    /// Immutable after creation.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Gets the human-readable label for this trigger.
    /// Must be between 1 and 150 characters and must not be blank.
    /// Example: "Code Review on Develop → Review".
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the foreign key of the source <see cref="BoardStatus"/> for the transition.
    /// Must differ from <see cref="ToStatusId"/>.
    /// </summary>
    public Guid FromStatusId { get; private set; }

    /// <summary>
    /// Gets the foreign key of the destination <see cref="BoardStatus"/> for the transition.
    /// Must differ from <see cref="FromStatusId"/>.
    /// </summary>
    public Guid ToStatusId { get; private set; }

    /// <summary>
    /// Gets the shared prompt sent to all agents when this trigger fires.
    /// Must be between 1 and 10 000 characters and must not be blank.
    /// </summary>
    public string Prompt { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this trigger is active.
    /// Disabled triggers are skipped during trigger evaluation.
    /// Defaults to <see langword="true"/> on creation.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this trigger was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp indicating when this trigger was last modified.
    /// Updated on every mutation.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the ordered list of agents associated with this trigger.
    /// Agents are executed sequentially in ascending <see cref="WorkflowTriggerAgent.SortOrder"/>.
    /// </summary>
    public IReadOnlyList<WorkflowTriggerAgent> TriggerAgents => _triggerAgents;

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private WorkflowTrigger() { }

    /// <summary>
    /// Creates a new <see cref="WorkflowTrigger"/> with the specified properties.
    /// <see cref="IsEnabled"/> is set to <see langword="true"/> and both timestamp fields are
    /// set to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    /// <param name="projectId">The identifier of the owning project.</param>
    /// <param name="name">
    /// The trigger label. Must not be <see langword="null"/> or whitespace and must be between
    /// 1 and 150 characters.
    /// </param>
    /// <param name="fromStatusId">
    /// The source <see cref="BoardStatus"/> identifier. Must differ from
    /// <paramref name="toStatusId"/>.
    /// </param>
    /// <param name="toStatusId">
    /// The destination <see cref="BoardStatus"/> identifier. Must differ from
    /// <paramref name="fromStatusId"/>.
    /// </param>
    /// <param name="prompt">
    /// The shared prompt for agents. Must not be <see langword="null"/> or whitespace and must
    /// be between 1 and 10 000 characters.
    /// </param>
    /// <returns>A new <see cref="WorkflowTrigger"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or
    /// exceeds 150 characters; when <paramref name="prompt"/> is <see langword="null"/>, empty,
    /// whitespace, or exceeds 10 000 characters; or when <paramref name="fromStatusId"/> equals
    /// <paramref name="toStatusId"/>.
    /// </exception>
    public static WorkflowTrigger Create(
        Guid projectId,
        string name,
        Guid fromStatusId,
        Guid toStatusId,
        string prompt)
    {
        ValidateName(name);
        ValidatePrompt(prompt);
        ValidateStatusTransition(fromStatusId, toStatusId);

        var now = DateTimeOffset.UtcNow;

        return new WorkflowTrigger
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            Prompt = prompt,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the trigger's mutable fields and refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    /// <param name="name">
    /// The new trigger label. Must not be <see langword="null"/> or whitespace and must be
    /// between 1 and 150 characters.
    /// </param>
    /// <param name="fromStatusId">
    /// The new source <see cref="BoardStatus"/> identifier. Must differ from
    /// <paramref name="toStatusId"/>.
    /// </param>
    /// <param name="toStatusId">
    /// The new destination <see cref="BoardStatus"/> identifier. Must differ from
    /// <paramref name="fromStatusId"/>.
    /// </param>
    /// <param name="prompt">
    /// The new shared prompt. Must not be <see langword="null"/> or whitespace and must be
    /// between 1 and 10 000 characters.
    /// </param>
    /// <param name="isEnabled">Whether the trigger should be active after the update.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, whitespace, or
    /// exceeds 150 characters; when <paramref name="prompt"/> is <see langword="null"/>, empty,
    /// whitespace, or exceeds 10 000 characters; or when <paramref name="fromStatusId"/> equals
    /// <paramref name="toStatusId"/>.
    /// </exception>
    public void Update(
        string name,
        Guid fromStatusId,
        Guid toStatusId,
        string prompt,
        bool isEnabled)
    {
        ValidateName(name);
        ValidatePrompt(prompt);
        ValidateStatusTransition(fromStatusId, toStatusId);

        Name = name;
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
        Prompt = prompt;
        IsEnabled = isEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enables this trigger so it participates in trigger evaluation.
    /// Refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Disables this trigger so it is skipped during trigger evaluation.
    /// Refreshes the <see cref="UpdatedAt"/> timestamp.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Replaces the entire agent list for this trigger with the supplied agents and refreshes
    /// the <see cref="UpdatedAt"/> timestamp.
    /// Intended for use by command handlers that rebuild the agent list on each update.
    /// </summary>
    /// <param name="agents">
    /// The new ordered set of <see cref="WorkflowTriggerAgent"/> instances to associate with
    /// this trigger. The collection is enumerated once; passing an empty sequence clears all
    /// agents.
    /// </param>
    public void SetAgents(IEnumerable<WorkflowTriggerAgent> agents)
    {
        _triggerAgents.Clear();
        _triggerAgents.AddRange(agents);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workflow trigger name must not be null or whitespace.", nameof(name));
        }

        if (name.Length > 150)
        {
            throw new ArgumentException("Workflow trigger name must not exceed 150 characters.", nameof(name));
        }
    }

    private static void ValidatePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Workflow trigger prompt must not be null or whitespace.", nameof(prompt));
        }

        if (prompt.Length > 10_000)
        {
            throw new ArgumentException("Workflow trigger prompt must not exceed 10 000 characters.", nameof(prompt));
        }
    }

    private static void ValidateStatusTransition(Guid fromStatusId, Guid toStatusId)
    {
        if (fromStatusId == toStatusId)
        {
            throw new ArgumentException("Source and destination status must be different.");
        }
    }
}
