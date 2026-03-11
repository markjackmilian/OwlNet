namespace OwlNet.Domain.Enums;

/// <summary>
/// Represents the priority level of a <c>Card</c> on the Kanban board.
/// Values are ordered from highest to lowest urgency.
/// </summary>
public enum CardPriority
{
    /// <summary>
    /// The card requires immediate attention and blocks other work.
    /// </summary>
    Critical,

    /// <summary>
    /// The card is important and should be addressed as soon as possible.
    /// </summary>
    High,

    /// <summary>
    /// The card has normal priority and should be addressed in the regular workflow.
    /// </summary>
    Medium,

    /// <summary>
    /// The card is a nice-to-have and can be deferred without significant impact.
    /// </summary>
    Low,
}
