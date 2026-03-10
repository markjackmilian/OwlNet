using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="AgentFileService"/> covering directory discovery,
/// file reading, frontmatter parsing, writing, and deletion scenarios.
/// All filesystem operations are mocked via <see cref="IFileSystem"/>.
/// </summary>
public sealed class AgentFileServiceTests
{
    private const string ProjectPath = @"C:\Projects\TestProject";

    /// <summary>
    /// Matches the production code's <c>Path.Combine(projectPath, ".opencode/agents")</c>
    /// which on Windows produces a mixed-separator path.
    /// </summary>
    private static readonly string AgentsDir =
        Path.Combine(ProjectPath, ".opencode/agents");

    private readonly IFileSystem _fileSystem;
    private readonly AgentFileService _sut;

    public AgentFileServiceTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _sut = new AgentFileService(_fileSystem, NullLogger<AgentFileService>.Instance);
    }

    /// <summary>
    /// Builds a file path the same way the production code does:
    /// <c>Path.Combine(agentsDir, fileName)</c>.
    /// </summary>
    private static string AgentFilePath(string fileName) =>
        Path.Combine(AgentsDir, fileName);

    // ──────────────────────────────────────────────
    // GetAgentsAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAgentsAsync_DirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        _fileSystem.DirectoryExists(AgentsDir).Returns(false);

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAgentsAsync_DirectoryExistsWithValidFiles_ReturnsAgentDtos()
    {
        // Arrange
        var file1Path = AgentFilePath("owl-coder.md");
        var file2Path = AgentFilePath("owl-tester.md");
        var file1Content = "---\nmode: primary\ndescription: Backend coder\n---\n# Coder body";
        var file2Content = "---\nmode: secondary\ndescription: Test writer\n---\n# Tester body";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([file1Path, file2Path]);
        _fileSystem.ReadAllTextAsync(file1Path, Arg.Any<CancellationToken>()).Returns(Task.FromResult(file1Content));
        _fileSystem.ReadAllTextAsync(file2Path, Arg.Any<CancellationToken>()).Returns(Task.FromResult(file2Content));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);

        result[0].ShouldSatisfyAllConditions(
            () => result[0].FileName.ShouldBe("owl-coder"),
            () => result[0].FilePath.ShouldBe(file1Path),
            () => result[0].Mode.ShouldBe("primary"),
            () => result[0].Description.ShouldBe("Backend coder"),
            () => result[0].RawContent.ShouldBe(file1Content)
        );

        result[1].ShouldSatisfyAllConditions(
            () => result[1].FileName.ShouldBe("owl-tester"),
            () => result[1].FilePath.ShouldBe(file2Path),
            () => result[1].Mode.ShouldBe("secondary"),
            () => result[1].Description.ShouldBe("Test writer"),
            () => result[1].RawContent.ShouldBe(file2Content)
        );
    }

    [Fact]
    public async Task GetAgentsAsync_ResultsSortedAlphabetically()
    {
        // Arrange — files returned in reverse alphabetical order
        var fileZPath = AgentFilePath("zebra.md");
        var fileAPath = AgentFilePath("alpha.md");
        var fileMPath = AgentFilePath("middle.md");

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([fileZPath, fileAPath, fileMPath]);
        _fileSystem.ReadAllTextAsync(fileZPath, Arg.Any<CancellationToken>()).Returns(Task.FromResult("---\nmode: z\n---"));
        _fileSystem.ReadAllTextAsync(fileAPath, Arg.Any<CancellationToken>()).Returns(Task.FromResult("---\nmode: a\n---"));
        _fileSystem.ReadAllTextAsync(fileMPath, Arg.Any<CancellationToken>()).Returns(Task.FromResult("---\nmode: m\n---"));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert — sorted by FileName ascending
        result.Count.ShouldBe(3);
        result[0].FileName.ShouldBe("alpha");
        result[1].FileName.ShouldBe("middle");
        result[2].FileName.ShouldBe("zebra");
    }

    [Fact]
    public async Task GetAgentsAsync_FileWithNoFrontmatter_ReturnsEmptyModeAndDescription()
    {
        // Arrange — file content has no frontmatter delimiters
        var filePath = AgentFilePath("plain-agent.md");
        var content = "# Just a heading\nSome body text without frontmatter.";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([filePath]);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldSatisfyAllConditions(
            () => result[0].FileName.ShouldBe("plain-agent"),
            () => result[0].Mode.ShouldBe(string.Empty),
            () => result[0].Description.ShouldBe(string.Empty),
            () => result[0].RawContent.ShouldBe(content)
        );
    }

    [Fact]
    public async Task GetAgentsAsync_FileWithMalformedFrontmatter_ReturnsDefaultValues()
    {
        // Arrange — frontmatter with no valid key:value pairs
        var filePath = AgentFilePath("broken.md");
        var content = "---\nthis is not valid yaml at all\nno colons here\n---\n# Body";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([filePath]);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldSatisfyAllConditions(
            () => result[0].Mode.ShouldBe(string.Empty),
            () => result[0].Description.ShouldBe(string.Empty)
        );
    }

    [Fact]
    public async Task GetAgentsAsync_EmptyFile_ReturnsEmptyFields()
    {
        // Arrange — zero-byte file content
        var filePath = AgentFilePath("empty.md");
        var content = "";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([filePath]);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldSatisfyAllConditions(
            () => result[0].FileName.ShouldBe("empty"),
            () => result[0].Mode.ShouldBe(string.Empty),
            () => result[0].Description.ShouldBe(string.Empty),
            () => result[0].RawContent.ShouldBe(content)
        );
    }

    [Fact]
    public async Task GetAgentsAsync_FileReadThrows_SkipsFileAndContinues()
    {
        // Arrange — first file throws, second file is valid
        var badFilePath = AgentFilePath("bad-file.md");
        var goodFilePath = AgentFilePath("good-file.md");
        var goodContent = "---\nmode: active\ndescription: Works fine\n---";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md").Returns([badFilePath, goodFilePath]);
        _fileSystem.ReadAllTextAsync(badFilePath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("File locked"));
        _fileSystem.ReadAllTextAsync(goodFilePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(goodContent));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert — bad file skipped, good file returned
        result.Count.ShouldBe(1);
        result[0].FileName.ShouldBe("good-file");
    }

    [Fact]
    public async Task GetAgentsAsync_GetFilesThrows_ReturnsEmptyList()
    {
        // Arrange — GetFiles throws an exception
        _fileSystem.DirectoryExists(AgentsDir).Returns(true);
        _fileSystem.GetFiles(AgentsDir, "*.md")
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _sut.GetAgentsAsync(ProjectPath, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────
    // GetAgentAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAgentAsync_FileExists_ReturnsAgentDto()
    {
        // Arrange
        var agentName = "owl-coder";
        var filePath = AgentFilePath("owl-coder.md");
        var content = "---\nmode: primary\ndescription: Backend developer\n---\n# Coder instructions";

        _fileSystem.FileExists(filePath).Returns(true);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            () => result.FileName.ShouldBe("owl-coder"),
            () => result.FilePath.ShouldBe(filePath),
            () => result.Mode.ShouldBe("primary"),
            () => result.Description.ShouldBe("Backend developer"),
            () => result.RawContent.ShouldBe(content)
        );
    }

    [Fact]
    public async Task GetAgentAsync_FileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var agentName = "nonexistent";
        var filePath = AgentFilePath("nonexistent.md");

        _fileSystem.FileExists(filePath).Returns(false);

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAgentAsync_ReadThrows_ReturnsNull()
    {
        // Arrange
        var agentName = "broken";
        var filePath = AgentFilePath("broken.md");

        _fileSystem.FileExists(filePath).Returns(true);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Disk read error"));

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    // ──────────────────────────────────────────────
    // WriteAgentAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task WriteAgentAsync_DirectoryDoesNotExist_CreatesDirectoryAndWritesFile()
    {
        // Arrange
        var agentName = "new-agent";
        var filePath = AgentFilePath("new-agent.md");
        var content = "---\nmode: custom\n---\n# New agent";

        _fileSystem.DirectoryExists(AgentsDir).Returns(false);

        // Act
        await _sut.WriteAgentAsync(ProjectPath, agentName, content, CancellationToken.None);

        // Assert
        _fileSystem.Received(1).CreateDirectory(AgentsDir);
        await _fileSystem.Received(1).WriteAllTextAsync(filePath, content, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WriteAgentAsync_DirectoryExists_WritesFileWithoutCreatingDirectory()
    {
        // Arrange
        var agentName = "existing-agent";
        var filePath = AgentFilePath("existing-agent.md");
        var content = "---\nmode: primary\n---\n# Existing agent";

        _fileSystem.DirectoryExists(AgentsDir).Returns(true);

        // Act
        await _sut.WriteAgentAsync(ProjectPath, agentName, content, CancellationToken.None);

        // Assert
        _fileSystem.DidNotReceive().CreateDirectory(Arg.Any<string>());
        await _fileSystem.Received(1).WriteAllTextAsync(filePath, content, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteAgentAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAgentAsync_FileExists_DeletesFile()
    {
        // Arrange
        var agentName = "old-agent";
        var filePath = AgentFilePath("old-agent.md");

        _fileSystem.FileExists(filePath).Returns(true);

        // Act
        await _sut.DeleteAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        _fileSystem.Received(1).DeleteFile(filePath);
    }

    [Fact]
    public async Task DeleteAgentAsync_FileDoesNotExist_ReturnsSilently()
    {
        // Arrange
        var agentName = "ghost-agent";
        var filePath = AgentFilePath("ghost-agent.md");

        _fileSystem.FileExists(filePath).Returns(false);

        // Act
        await _sut.DeleteAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        _fileSystem.DidNotReceive().DeleteFile(Arg.Any<string>());
    }

    // ──────────────────────────────────────────────
    // Frontmatter parsing (tested through GetAgentAsync)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAgentAsync_FrontmatterWithExtraWhitespace_ParsesCorrectly()
    {
        // Arrange — frontmatter keys/values have extra whitespace
        var agentName = "whitespace-agent";
        var filePath = AgentFilePath("whitespace-agent.md");
        var content = "---\n  mode:  primary  \n  description:  A spaced description  \n---\n# Body";

        _fileSystem.FileExists(filePath).Returns(true);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            () => result.Mode.ShouldBe("primary"),
            () => result.Description.ShouldBe("A spaced description")
        );
    }

    [Fact]
    public async Task GetAgentAsync_FrontmatterModeUnknownValue_ReturnsRawValue()
    {
        // Arrange — mode has a non-standard value
        var agentName = "custom-mode";
        var filePath = AgentFilePath("custom-mode.md");
        var content = "---\nmode: custom-value\ndescription: Custom agent\n---";

        _fileSystem.FileExists(filePath).Returns(true);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Mode.ShouldBe("custom-value");
    }

    [Fact]
    public async Task GetAgentAsync_FrontmatterOnlyOpeningDelimiter_ParsesUntilEof()
    {
        // Arrange — only one `---` line, no closing delimiter
        var agentName = "no-close";
        var filePath = AgentFilePath("no-close.md");
        var content = "---\nmode: orphan\ndescription: No closing delimiter";

        _fileSystem.FileExists(filePath).Returns(true);
        _fileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(Task.FromResult(content));

        // Act
        var result = await _sut.GetAgentAsync(ProjectPath, agentName, CancellationToken.None);

        // Assert — parser reads until EOF when no closing delimiter, so values are still extracted
        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            () => result.Mode.ShouldBe("orphan"),
            () => result.Description.ShouldBe("No closing delimiter")
        );
    }
}
