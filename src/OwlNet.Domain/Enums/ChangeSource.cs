namespace OwlNet.Domain.Enums;

/// <summary>
/// Indicates the actor or mechanism responsible for a <c>CardStatusHistory</c> transition.
/// </summary>
public enum ChangeSource
{
    /// <summary>
    /// The status change was performed explicitly by a human user through the UI.
    /// </summary>
    Manual,

    /// <summary>
    /// The status change was performed automatically by a workflow trigger.
    /// </summary>
    Trigger,
}
