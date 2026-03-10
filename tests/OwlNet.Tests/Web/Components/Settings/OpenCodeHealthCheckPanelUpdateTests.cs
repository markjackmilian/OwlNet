using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Web.Components.Settings;

// ═══════════════════════════════════════════════════════════════════════════════
//  Helper types — replicate the update logic from OpenCodeHealthCheckPanel.razor
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Possible outcomes of a <c>CheckForUpdatesAsync</c> call.
/// Mirrors the relevant subset of <c>HealthCheckState</c> in the Blazor component.
/// </summary>
internal enum UpdateCheckResult
{
    /// <summary>No check has been performed yet.</summary>
    None,

    /// <summary>The installed version matches the latest NPM version.</summary>
    UpToDate,

    /// <summary>A newer version is available on NPM.</summary>
    UpdateAvailable,

    /// <summary><c>npm view</c> failed (network error, timeout, etc.).</summary>
    Failed
}

/// <summary>
/// Possible outcomes of an <c>UpdateAsync</c> call used by <see cref="UpdateLogicSimulator"/>.
/// Mirrors the relevant subset of <c>HealthCheckState</c> in the Blazor component.
/// </summary>
internal enum UpdateOutcome
{
    /// <summary>No update has been attempted yet.</summary>
    None,

    /// <summary>The update command succeeded and the subsequent version check also succeeded.</summary>
    Success,

    /// <summary>The update command succeeded but the subsequent version check failed.</summary>
    VersionCheckFailed,

    /// <summary>The update command itself failed.</summary>
    Failed
}

/// <summary>
/// Replicates the pure business logic of <c>CheckForUpdatesAsync</c> and
/// <c>UpdateAsync</c> from <c>OpenCodeHealthCheckPanel.razor</c> in a plain C#
/// class so it can be exercised without a Blazor rendering host.
///
/// The logic is kept intentionally identical to the component — any divergence
/// would make the tests meaningless.
/// </summary>
internal sealed class UpdateLogicSimulator
{
    private readonly ICliService _cliService;

    /// <summary>The currently installed OpenCode version (set before calling <see cref="CheckForUpdatesAsync"/>).</summary>
    public string InstalledVersion { get; set; } = string.Empty;

    /// <summary>The latest version returned by <c>npm view opencode-ai version</c>.</summary>
    public string LatestVersion { get; private set; } = string.Empty;

    /// <summary>The error output captured when an update command fails.</summary>
    public string UpdateErrorOutput { get; private set; } = string.Empty;

    /// <summary>The result of the last <see cref="CheckForUpdatesAsync"/> call.</summary>
    public UpdateCheckResult CheckResult { get; private set; }

    /// <summary>The result of the last <see cref="UpdateAsync"/> call.</summary>
    public UpdateOutcome UpdateResult { get; private set; }

    /// <summary>Initialises the simulator with the <see cref="ICliService"/> to use.</summary>
    public UpdateLogicSimulator(ICliService cliService) => _cliService = cliService;

    /// <summary>
    /// Replicates <c>CheckForUpdatesAsync</c> from the component:
    /// runs <c>npm view opencode-ai version</c> (15 s timeout) and compares
    /// the trimmed output with <see cref="InstalledVersion"/>.
    /// </summary>
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        LatestVersion = string.Empty; // Mirror component: _latestVersion is reset before re-check
        var result = await _cliService.RunCommandAsync("npm", "view opencode-ai version", 15_000, ct);

        if (!result.IsSuccess)
        {
            CheckResult = UpdateCheckResult.Failed;
            return;
        }

        var latestVersion = result.Output.Trim();

        // Empty output from a successful exit code is a format mismatch → treat as failure
        if (string.IsNullOrWhiteSpace(latestVersion)) { CheckResult = UpdateCheckResult.Failed; return; }

        if (string.Equals(InstalledVersion.Trim(), latestVersion, StringComparison.Ordinal))
        {
            CheckResult = UpdateCheckResult.UpToDate;
        }
        else
        {
            LatestVersion = latestVersion;
            CheckResult = UpdateCheckResult.UpdateAvailable;
        }
    }

    /// <summary>
    /// Replicates <c>UpdateAsync</c> from the component:
    /// runs <c>npm i -g opencode-ai@latest</c> (120 s timeout); on success
    /// re-runs <c>opencode --version</c> (10 s) to confirm the new version.
    /// </summary>
    public async Task UpdateAsync(CancellationToken ct = default)
    {
        var updateResult = await _cliService.RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, ct);

        if (updateResult.IsSuccess)
        {
            var versionResult = await _cliService.RunCommandAsync("opencode", "--version", 10_000, ct);
            // In the real component, a failed post-update version check transitions to
            // NotInstalled (via CheckOpenCodeVersionAsync). VersionCheckFailed is a
            // simulator-only value that serves as a proxy for "not Success" in tests,
            // allowing the test to assert that the success snackbar is NOT shown.
            UpdateResult = versionResult.IsSuccess ? UpdateOutcome.Success : UpdateOutcome.VersionCheckFailed;
        }
        else
        {
            UpdateErrorOutput = !string.IsNullOrWhiteSpace(updateResult.Error)
                ? updateResult.Error
                : !string.IsNullOrWhiteSpace(updateResult.Output)
                    ? updateResult.Output
                    : "Update failed with no output. Try running the command manually.";
            UpdateResult = UpdateOutcome.Failed;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Test class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unit tests for the update-management logic of <c>OpenCodeHealthCheckPanel.razor</c>.
///
/// Because the component methods are private and live inside a Blazor <c>@code</c>
/// block, the tests exercise an <see cref="UpdateLogicSimulator"/> that replicates
/// the identical logic using a mocked <see cref="ICliService"/>.  This lets us
/// verify every CLI call sequence and state transition without a rendering host.
///
/// Spec reference: <c>specs/todo/SPEC-opencode-update-management.md</c>
/// </summary>
public sealed class OpenCodeHealthCheckPanelUpdateTests
{
    private readonly ICliService _cliService;

    /// <summary>
    /// Creates a fresh <see cref="ICliService"/> substitute for every test.
    /// xUnit instantiates a new test-class instance per test method, so there
    /// is no shared mutable state between tests.
    /// </summary>
    public OpenCodeHealthCheckPanelUpdateTests()
    {
        _cliService = Substitute.For<ICliService>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CheckForUpdatesAsync — happy paths
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>npm view opencode-ai version</c> succeeds and the returned version
    /// matches the installed version, the result must be <see cref="UpdateCheckResult.UpToDate"/>.
    /// </summary>
    [Fact]
    public async Task CheckForUpdates_NpmViewSucceeds_SameVersion_TransitionsToUpToDate()
    {
        // Arrange
        const string installedVersion = "1.2.3";
        const string latestVersion = "1.2.3";

        _cliService
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, latestVersion, "")));

        var simulator = new UpdateLogicSimulator(_cliService)
        {
            InstalledVersion = installedVersion
        };

        // Act
        await simulator.CheckForUpdatesAsync(CancellationToken.None);

        // Assert
        simulator.ShouldSatisfyAllConditions(
            () => simulator.CheckResult.ShouldBe(UpdateCheckResult.UpToDate),
            () => simulator.LatestVersion.ShouldBe(string.Empty) // not populated when up-to-date
        );

        await _cliService.Received(1)
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When <c>npm view opencode-ai version</c> succeeds and the returned version
    /// is newer than the installed version, the result must be
    /// <see cref="UpdateCheckResult.UpdateAvailable"/> and <c>LatestVersion</c>
    /// must be populated with the new version string.
    /// </summary>
    [Fact]
    public async Task CheckForUpdates_NpmViewSucceeds_NewerVersion_TransitionsToUpdateAvailable()
    {
        // Arrange
        const string installedVersion = "1.2.3";
        const string latestVersion = "1.3.0";

        _cliService
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, latestVersion, "")));

        var simulator = new UpdateLogicSimulator(_cliService)
        {
            InstalledVersion = installedVersion
        };

        // Act
        await simulator.CheckForUpdatesAsync(CancellationToken.None);

        // Assert
        simulator.ShouldSatisfyAllConditions(
            () => simulator.CheckResult.ShouldBe(UpdateCheckResult.UpdateAvailable),
            () => simulator.LatestVersion.ShouldBe(latestVersion)
        );

        await _cliService.Received(1)
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CheckForUpdatesAsync — failure / edge cases
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>npm view opencode-ai version</c> exits with a non-zero code
    /// (e.g. network error or timeout), the result must be
    /// <see cref="UpdateCheckResult.Failed"/>.
    /// </summary>
    [Fact]
    public async Task CheckForUpdates_NpmViewFails_TransitionsToUpdateCheckFailed()
    {
        // Arrange
        _cliService
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(1, "", "npm ERR! network timeout")));

        var simulator = new UpdateLogicSimulator(_cliService)
        {
            InstalledVersion = "1.2.3"
        };

        // Act
        await simulator.CheckForUpdatesAsync(CancellationToken.None);

        // Assert
        simulator.CheckResult.ShouldBe(UpdateCheckResult.Failed);

        await _cliService.Received(1)
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the NPM output contains surrounding whitespace (e.g. "  1.2.3  "),
    /// the comparison must trim both sides before comparing so that a version
    /// that is logically equal is not treated as an update.
    /// </summary>
    [Fact]
    public async Task CheckForUpdates_NpmViewOutputWithWhitespace_TrimsBeforeComparing()
    {
        // Arrange — output has leading and trailing spaces
        const string installedVersion = "1.2.3";
        const string npmOutputWithSpaces = "  1.2.3  ";

        _cliService
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, npmOutputWithSpaces, "")));

        var simulator = new UpdateLogicSimulator(_cliService)
        {
            InstalledVersion = installedVersion
        };

        // Act
        await simulator.CheckForUpdatesAsync(CancellationToken.None);

        // Assert — trimmed "1.2.3" == "1.2.3" → UpToDate, not UpdateAvailable
        simulator.CheckResult.ShouldBe(UpdateCheckResult.UpToDate);
    }

    /// <summary>
    /// When <c>npm view</c> exits with code 0 but returns an empty output string,
    /// the trimmed output is <c>""</c> which is a format mismatch (the command
    /// succeeded but produced no usable version string).  The component treats
    /// this as <see cref="UpdateCheckResult.Failed"/> rather than
    /// <see cref="UpdateCheckResult.UpdateAvailable"/>, because an empty version
    /// string cannot be meaningfully compared or displayed.
    /// </summary>
    [Fact]
    public async Task CheckForUpdates_EmptyOutput_TreatedAsUpdateCheckFailed()
    {
        // Arrange
        const string installedVersion = "1.2.3";

        _cliService
            .RunCommandAsync("npm", "view opencode-ai version", 15_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, "", "")));

        var simulator = new UpdateLogicSimulator(_cliService)
        {
            InstalledVersion = installedVersion
        };

        // Act
        await simulator.CheckForUpdatesAsync(CancellationToken.None);

        // Assert — empty output with ExitCode 0 is a format mismatch → Failed
        simulator.CheckResult.ShouldBe(UpdateCheckResult.Failed);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UpdateAsync — happy paths
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>npm i -g opencode-ai@latest</c> succeeds, the simulator must
    /// subsequently call <c>opencode --version</c> to re-verify the installation.
    /// Both calls must use the correct arguments and timeouts.
    /// </summary>
    [Fact]
    public async Task Update_Succeeds_CallsVersionCheckAfterUpdate()
    {
        // Arrange
        _cliService
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, "added 1 package", "")));

        _cliService
            .RunCommandAsync("opencode", "--version", 10_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, "1.3.0", "")));

        var simulator = new UpdateLogicSimulator(_cliService);

        // Act
        await simulator.UpdateAsync(CancellationToken.None);

        // Assert
        simulator.UpdateResult.ShouldBe(UpdateOutcome.Success);

        await _cliService.Received(1)
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>());

        await _cliService.Received(1)
            .RunCommandAsync("opencode", "--version", 10_000, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UpdateAsync — failure scenarios and error-output fallback chain
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When the update command fails and the <c>Error</c> stream is non-empty,
    /// <c>UpdateErrorOutput</c> must be populated from <c>Error</c> (first priority).
    /// </summary>
    [Fact]
    public async Task Update_Fails_WithErrorOutput_CapturesErrorOutput()
    {
        // Arrange
        const string errorMessage = "EACCES permission denied";

        _cliService
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(1, "", errorMessage)));

        var simulator = new UpdateLogicSimulator(_cliService);

        // Act
        await simulator.UpdateAsync(CancellationToken.None);

        // Assert
        simulator.ShouldSatisfyAllConditions(
            () => simulator.UpdateResult.ShouldBe(UpdateOutcome.Failed),
            () => simulator.UpdateErrorOutput.ShouldBe(errorMessage)
        );
    }

    /// <summary>
    /// When the update command fails with an empty <c>Error</c> stream but a
    /// non-empty <c>Output</c> stream, <c>UpdateErrorOutput</c> must fall back
    /// to <c>Output</c> (second priority).
    /// </summary>
    [Fact]
    public async Task Update_Fails_WithNoErrorButOutput_CapturesOutput()
    {
        // Arrange
        const string outputMessage = "npm ERR! something";

        _cliService
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(1, outputMessage, "")));

        var simulator = new UpdateLogicSimulator(_cliService);

        // Act
        await simulator.UpdateAsync(CancellationToken.None);

        // Assert
        simulator.ShouldSatisfyAllConditions(
            () => simulator.UpdateResult.ShouldBe(UpdateOutcome.Failed),
            () => simulator.UpdateErrorOutput.ShouldBe(outputMessage)
        );
    }

    /// <summary>
    /// When the update command fails with both <c>Error</c> and <c>Output</c>
    /// empty, <c>UpdateErrorOutput</c> must fall back to the generic message
    /// (third priority / last resort).
    /// </summary>
    [Fact]
    public async Task Update_Fails_WithNoOutput_UsesGenericMessage()
    {
        // Arrange
        const string expectedGenericMessage =
            "Update failed with no output. Try running the command manually.";

        _cliService
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(1, "", "")));

        var simulator = new UpdateLogicSimulator(_cliService);

        // Act
        await simulator.UpdateAsync(CancellationToken.None);

        // Assert
        simulator.ShouldSatisfyAllConditions(
            () => simulator.UpdateResult.ShouldBe(UpdateOutcome.Failed),
            () => simulator.UpdateErrorOutput.ShouldBe(expectedGenericMessage)
        );
    }

    /// <summary>
    /// When the update command succeeds but the subsequent <c>opencode --version</c>
    /// check fails, the result must be <see cref="UpdateOutcome.VersionCheckFailed"/>
    /// (not <see cref="UpdateOutcome.Success"/>).  This prevents a false-positive
    /// success snackbar from being shown in the component.
    /// </summary>
    [Fact]
    public async Task Update_Succeeds_ButVersionCheckFails_DoesNotShowSuccessState()
    {
        // Arrange
        _cliService
            .RunCommandAsync("npm", "i -g opencode-ai@latest", 120_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(0, "added 1 package", "")));

        _cliService
            .RunCommandAsync("opencode", "--version", 10_000, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CliResult(1, "", "opencode: command not found")));

        var simulator = new UpdateLogicSimulator(_cliService);

        // Act
        await simulator.UpdateAsync(CancellationToken.None);

        // Assert — version check failed, so the final state is NOT Success
        simulator.ShouldSatisfyAllConditions(
            () => simulator.UpdateResult.ShouldNotBe(UpdateOutcome.Success),
            () => simulator.UpdateResult.ShouldBe(UpdateOutcome.VersionCheckFailed)
        );
    }
}
