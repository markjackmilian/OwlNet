namespace OwlNet.Web.Components.Projects.Board;

/// <summary>
/// Represents a single card on the project Kanban board.
/// Mutable class because MudDropContainer updates the <see cref="Status"/> property on drag-and-drop.
/// </summary>
public sealed class BoardCardItem
{
    /// <summary>Unique card identifier.</summary>
    public int Id { get; set; }

    /// <summary>Card title displayed prominently (max 2 lines, truncated with ellipsis).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Card description in Markdown format. Rendered as HTML in the card view.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Priority level — determines the badge color.</summary>
    public CardPriority Priority { get; set; }

    /// <summary>Current board column — updated when the card is dropped in a new zone.</summary>
    public BoardStatus Status { get; set; }
}
