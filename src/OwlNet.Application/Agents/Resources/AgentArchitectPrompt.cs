using System.Reflection;

namespace OwlNet.Application.Agents.Resources;

/// <summary>
/// Provides access to the embedded Agent Architect system prompt used by the
/// agent creation wizard to instruct the LLM on generating OpenCode-compatible
/// agent definitions.
/// </summary>
internal static class AgentArchitectPrompt
{
    private static readonly string ResourceName =
        "OwlNet.Application.Agents.Resources.AgentArchitectSystemPrompt.txt";

    private static readonly Lazy<string> _cachedPrompt = new(LoadFromResource);

    /// <summary>
    /// Returns the Agent Architect system prompt from the embedded resource.
    /// The result is cached after the first call so the resource is read only once.
    /// </summary>
    /// <returns>The full system prompt text.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be found in the assembly.
    /// </exception>
    internal static string GetSystemPrompt() => _cachedPrompt.Value;

    /// <summary>
    /// Loads the system prompt text from the embedded assembly resource.
    /// </summary>
    private static string LoadFromResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found. " +
                "Ensure the file is marked as an EmbeddedResource in the project file.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
