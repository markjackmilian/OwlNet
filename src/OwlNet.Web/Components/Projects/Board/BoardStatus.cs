namespace OwlNet.Web.Components.Projects.Board;

/// <summary>
/// Represents the workflow stages (columns) on the project Kanban board.
/// Each value maps to a MudDropZone identifier.
/// </summary>
public enum BoardStatus
{
    /// <summary>Items not yet scheduled for work.</summary>
    Backlog,

    /// <summary>Items awaiting triage or estimation.</summary>
    ToEvaluate,

    /// <summary>Items actively being developed.</summary>
    Develop,

    /// <summary>Items in code review or QA.</summary>
    Review,

    /// <summary>Items completed and verified.</summary>
    Done
}
