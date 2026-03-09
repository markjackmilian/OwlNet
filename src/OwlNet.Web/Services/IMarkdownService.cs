namespace OwlNet.Web.Services;

/// <summary>
/// Converts Markdown text to sanitized HTML.
/// Used to render card descriptions on the project board (FR-18, FR-19).
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts the specified Markdown string to an HTML string.
    /// Supports bold, italic, inline code, and unordered lists.
    /// </summary>
    /// <param name="markdown">
    /// The Markdown text to convert. If <see langword="null"/> or empty,
    /// returns <see cref="string.Empty"/>.
    /// </param>
    /// <returns>The rendered HTML string, or <see cref="string.Empty"/> if the input is null or empty.</returns>
    string ToHtml(string markdown);
}
