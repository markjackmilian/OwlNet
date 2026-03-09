namespace OwlNet.Web.Components.Projects.Board;

/// <summary>
/// Priority levels for board cards, each with a distinct visual color.
/// </summary>
public enum CardPriority
{
    /// <summary>Blocking issue requiring immediate attention — red tones.</summary>
    Critical,

    /// <summary>Important item that should be addressed soon — orange tones.</summary>
    High,

    /// <summary>Standard priority work item — blue tones.</summary>
    Medium,

    /// <summary>Nice-to-have improvement or minor task — green/neutral tones.</summary>
    Low
}
