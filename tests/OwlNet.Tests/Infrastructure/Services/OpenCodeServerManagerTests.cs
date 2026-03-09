using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Constants;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="OpenCodeServerManager"/>. All external dependencies
/// (IOpenCodeClient, IServiceScopeFactory, IAppSettingService) are substituted.
/// Tests focus on observable behaviors through interfaces: status transitions,
/// event firing, and correct URL resolution. Process-based methods (StartServerAsync)
/// are not tested because they require a real OS process.
/// </summary>
public sealed class OpenCodeServerManagerTests
{
    private readonly IOpenCodeClient _openCodeClient;
    private readonly IAppSettingService _appSettingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpenCodeServerManager _sut;

    public OpenCodeServerManagerTests()
    {
        _openCodeClient = Substitute.For<IOpenCodeClient>();
        (_scopeFactory, _appSettingService) = CreateScopeFactory(_openCodeClient);
        _sut = new OpenCodeServerManager(
            _scopeFactory,
            NullLogger<OpenCodeServerManager>.Instance);
    }

    /// <summary>
    /// Creates a mock <see cref="IServiceScopeFactory"/> that resolves substituted
    /// <see cref="IAppSettingService"/> and <see cref="IOpenCodeClient"/> from its scoped
    /// <see cref="IServiceProvider"/>. This mirrors the production DI pattern where the
    /// manager is a singleton but both services have shorter lifetimes and are resolved per-call.
    /// </summary>
    private static (IServiceScopeFactory scopeFactory, IAppSettingService appSettingService) CreateScopeFactory(
        IOpenCodeClient openCodeClient)
    {
        var appSettingService = Substitute.For<IAppSettingService>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IAppSettingService)).Returns(appSettingService);
        serviceProvider.GetService(typeof(IOpenCodeClient)).Returns(openCodeClient);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return (scopeFactory, appSettingService);
    }

    // ──────────────────────────────────────────────
    // CurrentStatus — initial state
    // ──────────────────────────────────────────────

    [Fact]
    public void CurrentStatus_Initially_IsUnknown()
    {
        // Arrange — fresh manager created in constructor

        // Act
        var status = _sut.CurrentStatus;

        // Assert
        status.ShouldSatisfyAllConditions(
            () => status.State.ShouldBe(OpenCodeServerState.Unknown),
            () => status.Version.ShouldBeNull(),
            () => status.ServerUrl.ShouldBeNull(),
            () => status.ErrorMessage.ShouldBeNull()
        );
    }

    // ──────────────────────────────────────────────
    // RefreshStatusAsync — health check succeeds
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckSucceeds_StatusBecomesRunning()
    {
        // Arrange
        var expectedVersion = "1.0.0";
        var healthResult = new OpenCodeHealthResult(IsHealthy: true, Version: expectedVersion);

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Success(healthResult)));

        // Act
        var result = await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.State.ShouldBe(OpenCodeServerState.Running),
            () => result.Version.ShouldBe(expectedVersion),
            () => result.ServerUrl.ShouldBe(OpenCodeConstants.DefaultServerUrl),
            () => result.ErrorMessage.ShouldBeNull()
        );

        _sut.CurrentStatus.State.ShouldBe(OpenCodeServerState.Running);
    }

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckSucceeds_NullVersion_UsesUnknownVersion()
    {
        // Arrange
        var healthResult = new OpenCodeHealthResult(IsHealthy: true, Version: null);

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Success(healthResult)));

        // Act
        var result = await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.State.ShouldBe(OpenCodeServerState.Running),
            () => result.Version.ShouldBe("unknown"),
            () => result.ServerUrl.ShouldBe(OpenCodeConstants.DefaultServerUrl)
        );
    }

    // ──────────────────────────────────────────────
    // RefreshStatusAsync — health check fails
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckFails_NoProcess_StatusBecomesStopped()
    {
        // Arrange
        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Failure("Connection refused")));

        // Act
        var result = await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.State.ShouldBe(OpenCodeServerState.Stopped),
            () => result.Version.ShouldBeNull(),
            () => result.ServerUrl.ShouldBeNull(),
            () => result.ErrorMessage.ShouldBeNull()
        );

        _sut.CurrentStatus.State.ShouldBe(OpenCodeServerState.Stopped);
    }

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckReturnsNotHealthy_NoProcess_StatusBecomesStopped()
    {
        // Arrange — health check succeeds at HTTP level but server reports unhealthy
        var healthResult = new OpenCodeHealthResult(IsHealthy: false, Version: null);

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Success(healthResult)));

        // Act
        var result = await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        result.State.ShouldBe(OpenCodeServerState.Stopped);
    }

    // ──────────────────────────────────────────────
    // RefreshStatusAsync — OnStatusChanged event
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckSucceeds_FiresOnStatusChanged()
    {
        // Arrange
        var healthResult = new OpenCodeHealthResult(IsHealthy: true, Version: "2.0.0");
        var eventFired = false;

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Success(healthResult)));

        _sut.OnStatusChanged += () => eventFired = true;

        // Act
        await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        eventFired.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshStatusAsync_HealthCheckFails_FiresOnStatusChanged()
    {
        // Arrange
        var eventFired = false;

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(OpenCodeConstants.DefaultServerUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Failure("Connection refused")));

        _sut.OnStatusChanged += () => eventFired = true;

        // Act
        await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        eventFired.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // RefreshStatusAsync — URL resolution
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshStatusAsync_UsesDefaultUrl_WhenSettingNotFound()
    {
        // Arrange
        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));

        _openCodeClient.HealthCheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Failure("Connection refused")));

        // Act
        await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        await _openCodeClient.Received(1).HealthCheckAsync(
            OpenCodeConstants.DefaultServerUrl,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshStatusAsync_UsesConfiguredUrl_WhenSettingExists()
    {
        // Arrange
        var configuredUrl = "http://localhost:9999";

        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Success(configuredUrl)));

        _openCodeClient.HealthCheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OpenCodeHealthResult>.Failure("Connection refused")));

        // Act
        await _sut.RefreshStatusAsync(CancellationToken.None);

        // Assert
        await _openCodeClient.Received(1).HealthCheckAsync(
            configuredUrl,
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // StopServerAsync — no process running
    // ──────────────────────────────────────────────

    [Fact]
    public async Task StopServerAsync_NoProcess_ReturnsSuccessAndStatusStopped()
    {
        // Arrange — fresh manager, no process started

        // Act
        var result = await _sut.StopServerAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _sut.CurrentStatus.ShouldSatisfyAllConditions(
            () => _sut.CurrentStatus.State.ShouldBe(OpenCodeServerState.Stopped),
            () => _sut.CurrentStatus.Version.ShouldBeNull(),
            () => _sut.CurrentStatus.ServerUrl.ShouldBeNull(),
            () => _sut.CurrentStatus.ErrorMessage.ShouldBeNull()
        );
    }

    [Fact]
    public async Task StopServerAsync_NoProcess_FiresOnStatusChanged()
    {
        // Arrange
        var eventFired = false;
        _sut.OnStatusChanged += () => eventFired = true;

        // Act
        await _sut.StopServerAsync(CancellationToken.None);

        // Assert
        eventFired.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────
    // Dispose
    // ──────────────────────────────────────────────

    [Fact]
    public void Dispose_FreshManager_DoesNotThrow()
    {
        // Arrange
        var manager = new OpenCodeServerManager(
            _scopeFactory,
            NullLogger<OpenCodeServerManager>.Instance);

        // Act & Assert
        Should.NotThrow(() => manager.Dispose());
    }
}
