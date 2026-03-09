namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents a request to execute a slash command in an OpenCode session.
/// </summary>
/// <param name="Command">The command name to execute (e.g., <c>"compact"</c>, <c>"plan"</c>).</param>
/// <param name="Args">An optional collection of command arguments, or <c>null</c> if no arguments are needed.</param>
public sealed record ExecuteCommandRequest(
    string Command,
    IReadOnlyList<string>? Args);
