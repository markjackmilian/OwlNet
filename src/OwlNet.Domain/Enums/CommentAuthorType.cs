namespace OwlNet.Domain.Enums;

/// <summary>
/// Identifies whether a <c>CardComment</c> was authored by a human user or an AI agent.
/// This distinction drives validation rules (which author identifier field is required)
/// and UI rendering (human vs. agent comment styling).
/// </summary>
public enum CommentAuthorType
{
    /// <summary>
    /// The comment was written by an authenticated human user.
    /// When this value is set, <c>CardComment.AuthorId</c> must be non-null and
    /// <c>CardComment.AgentName</c> must be <see langword="null"/>.
    /// </summary>
    Human = 0,

    /// <summary>
    /// The comment was posted by an AI agent, typically as part of a workflow trigger execution.
    /// When this value is set, <c>CardComment.AgentName</c> must be non-null and
    /// <c>CardComment.AuthorId</c> must be <see langword="null"/>.
    /// </summary>
    Agent = 1,
}
