using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Integration-style tests for <see cref="CliService"/> that execute real CLI commands
/// via <c>cmd.exe</c> on Windows. Each test verifies a specific behavior of the service
/// including success, failure, timeout, cancellation, and stream capture scenarios.
/// </summary>
public sealed class CliServiceTests
{
    private readonly CliService _sut;

    public CliServiceTests()
    {
        _sut = new CliService(NullLogger<CliService>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RunCommandAsync_ValidCommand_ReturnsSuccessWithOutput()
    {
        // Arrange
        var command = "echo";
        var arguments = "hello";

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.ExitCode.ShouldBe(0),
            () => result.Output.ShouldContain("hello")
        );
    }

    [Fact]
    public async Task RunCommandAsync_EmptyArguments_RunsCommandWithoutArguments()
    {
        // Arrange — "echo" with no arguments on Windows prints "ECHO is on."
        var command = "echo";
        var arguments = "";

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.ExitCode.ShouldBe(0),
            () => result.Output.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public async Task RunCommandAsync_OutputIsTrimmed_ReturnsCleanOutput()
    {
        // Arrange
        var command = "echo";
        var arguments = "hello";

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, cancellationToken: CancellationToken.None);

        // Assert — Output should be trimmed (no leading/trailing whitespace or newlines)
        result.ShouldSatisfyAllConditions(
            () => result.Output.ShouldBe(result.Output.Trim()),
            () => result.Output.ShouldBe("hello")
        );
    }

    // ──────────────────────────────────────────────
    // Failure scenarios
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RunCommandAsync_InvalidCommand_ReturnsFailureResult()
    {
        // Arrange — a command that does not exist on any system
        var command = "nonexistent_command_xyz_12345";

        // Act
        var result = await _sut.RunCommandAsync(command, cancellationToken: CancellationToken.None);

        // Assert — cmd.exe wrapping means we get a non-zero exit code rather than Win32Exception
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ExitCode.ShouldNotBe(0),
            () => result.Error.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public async Task RunCommandAsync_CommandWithNonZeroExitCode_ReturnsFailureResult()
    {
        // Arrange — "cmd /c exit 1" wrapped by BuildPlatformCommand becomes "cmd.exe /c cmd /c exit 1"
        // Simpler: use "exit" directly since BuildPlatformCommand wraps with cmd.exe /c
        var command = "exit";
        var arguments = "1";

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ExitCode.ShouldNotBe(0)
        );
    }

    // ──────────────────────────────────────────────
    // Stderr capture
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RunCommandAsync_CapturesStderr_ReturnsErrorOutput()
    {
        // Arrange — redirect stdout to stderr using cmd redirection
        var command = "echo";
        var arguments = "error_text 1>&2";

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, cancellationToken: CancellationToken.None);

        // Assert
        result.Error.ShouldContain("error_text");
    }

    // ──────────────────────────────────────────────
    // Timeout
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RunCommandAsync_Timeout_ReturnsTimeoutError()
    {
        // Arrange — ping with 30 attempts takes far longer than 500ms
        var command = "ping";
        var arguments = "-n 30 127.0.0.1";
        var shortTimeoutMs = 500;

        // Act
        var result = await _sut.RunCommandAsync(command, arguments, timeoutMs: shortTimeoutMs, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ExitCode.ShouldBe(-1),
            () => result.Error.ShouldContain("timed out")
        );
    }

    // ──────────────────────────────────────────────
    // Cancellation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RunCommandAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange — pass an already-cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await _sut.RunCommandAsync("echo", "hello", cancellationToken: cts.Token));
    }
}
