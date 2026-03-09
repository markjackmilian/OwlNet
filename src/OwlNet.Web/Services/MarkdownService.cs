using Markdig;

namespace OwlNet.Web.Services;

/// <summary>
/// Converts Markdown text to HTML using the Markdig library.
/// This service is stateless and thread-safe — suitable for singleton registration.
/// The <see cref="MarkdownPipeline"/> is built once and reused across all calls.
/// </summary>
public sealed class MarkdownService : IMarkdownService
{
    /// <summary>
    /// Pre-built Markdig pipeline with advanced extensions enabled.
    /// Includes support for bold, italic, inline code, unordered lists,
    /// task lists, tables, and other common Markdown features.
    /// Thread-safe and reused for every conversion call.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <inheritdoc />
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, Pipeline);
    }
}
