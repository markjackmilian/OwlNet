using OwlNet.Web.Services;
using Shouldly;

namespace OwlNet.Tests.Web.Services;

/// <summary>
/// Unit tests for <see cref="MarkdownService"/>.
/// Verifies Markdown-to-HTML conversion for card descriptions (FR-18, FR-19).
/// </summary>
public sealed class MarkdownServiceTests
{
    private readonly MarkdownService _sut = new();

    // ──────────────────────────────────────────────
    // Null / Empty / Whitespace Inputs
    // ──────────────────────────────────────────────

    [Fact]
    public void ToHtml_NullInput_ReturnsEmptyString()
    {
        // Arrange
        string markdown = null!;

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToHtml_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var markdown = string.Empty;

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToHtml_WhitespaceOnlyInput_DoesNotReturnNull()
    {
        // Arrange — whitespace passes the IsNullOrEmpty guard, so Markdig processes it.
        // Markdig strips pure whitespace and returns an empty string.
        var markdown = "   ";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldNotBeNull();
    }

    // ──────────────────────────────────────────────
    // Plain Text
    // ──────────────────────────────────────────────

    [Fact]
    public void ToHtml_PlainText_ReturnsWrappedInParagraph()
    {
        // Arrange
        var markdown = "hello";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldContain("<p>hello</p>");
    }

    // ──────────────────────────────────────────────
    // Inline Formatting
    // ──────────────────────────────────────────────

    [Fact]
    public void ToHtml_BoldText_ReturnsHtmlWithStrongTag()
    {
        // Arrange
        var markdown = "**bold text**";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldContain("<strong>bold text</strong>");
    }

    [Fact]
    public void ToHtml_ItalicText_ReturnsHtmlWithEmTag()
    {
        // Arrange
        var markdown = "*italic text*";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldContain("<em>italic text</em>");
    }

    [Fact]
    public void ToHtml_InlineCode_ReturnsHtmlWithCodeTag()
    {
        // Arrange
        var markdown = "`code snippet`";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldContain("<code>code snippet</code>");
    }

    // ──────────────────────────────────────────────
    // Lists
    // ──────────────────────────────────────────────

    [Fact]
    public void ToHtml_UnorderedList_ReturnsHtmlWithUlAndLiTags()
    {
        // Arrange
        var markdown = "- item1\n- item2";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldContain("<ul>"),
            () => result.ShouldContain("</ul>"),
            () => result.ShouldContain("<li>item1</li>"),
            () => result.ShouldContain("<li>item2</li>")
        );
    }

    // ──────────────────────────────────────────────
    // Mixed Markdown
    // ──────────────────────────────────────────────

    [Fact]
    public void ToHtml_MixedMarkdown_ReturnsCorrectHtml()
    {
        // Arrange — bold, italic, and inline code in the same text
        var markdown = "This is **bold**, *italic*, and `code` together.";

        // Act
        var result = _sut.ToHtml(markdown);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldContain("<strong>bold</strong>"),
            () => result.ShouldContain("<em>italic</em>"),
            () => result.ShouldContain("<code>code</code>")
        );
    }
}
