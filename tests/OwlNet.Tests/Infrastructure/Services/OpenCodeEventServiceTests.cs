using System.Net;
using System.Text;
using System.Text.Json;
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
/// Unit tests for <see cref="OpenCodeEventService"/>. All external dependencies
/// (HttpClient, IServiceScopeFactory, IOpenCodeServerManager) are substituted.
/// Tests cover: subscribe/unsubscribe, SSE event parsing and distribution,
/// consumer error handling, start/stop lifecycle, and disposal.
/// </summary>
public sealed class OpenCodeEventServiceTests : IAsyncLifetime
{
    private readonly IOpenCodeServerManager _serverManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppSettingService _appSettingService;
    private OpenCodeEventService? _sut;

    public OpenCodeEventServiceTests()
    {
        _serverManager = Substitute.For<IOpenCodeServerManager>();
        _serverManager.CurrentStatus.Returns(OpenCodeServerStatus.CreateUnknown());

        (_scopeFactory, _appSettingService) = CreateScopeFactory();

        // Default: setting not found → use default URL
        _appSettingService.GetByKeyAsync(OpenCodeConstants.ServerUrlSettingKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<string>.Failure("Setting not found")));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_sut is not null)
        {
            try
            {
                await _sut.StopAsync(CancellationToken.None);
            }
            catch
            {
                // Best effort cleanup
            }

            _sut.Dispose();
        }
    }

    // ── Subscribe / Unsubscribe ─────────────────────────────────────────

    [Fact]
    public void Subscribe_ValidEventType_ReturnsDisposable()
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));

        // Act
        var subscription = _sut.Subscribe("session.updated", _ => Task.CompletedTask);

        // Assert
        subscription.ShouldNotBeNull();
        subscription.ShouldBeAssignableTo<IDisposable>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  \t  ")]
    public void Subscribe_NullOrWhitespaceEventType_ThrowsArgumentException(string? eventType)
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => _sut.Subscribe(eventType!, _ => Task.CompletedTask));
    }

    [Fact]
    public void Subscribe_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => _sut.Subscribe("session.updated", null!));
    }

    [Fact]
    public async Task Subscribe_DisposingSubscription_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        var receivedSignal = new SemaphoreSlim(0, 1);

        var sseContent = BuildSseContent("session.updated", """{"id":"1"}""");
        var handler = new SseHttpMessageHandler(sseContent);
        _sut = CreateService(handler);

        var subscription = _sut.Subscribe("session.updated", _ =>
        {
            handlerCalled = true;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act — dispose the subscription before starting
        subscription.Dispose();

        await _sut.StartAsync(CancellationToken.None);
        var wasSignaled = await receivedSignal.WaitAsync(TimeSpan.FromMilliseconds(500));

        // Assert — handler should NOT have been called after disposal
        wasSignaled.ShouldBeFalse();
        handlerCalled.ShouldBeFalse();
    }

    // ── SSE Event Parsing & Distribution ────────────────────────────────

    [Fact]
    public async Task StartAsync_ReceivesServerConnectedEvent_SetsIsConnectedTrue()
    {
        // Arrange
        var connectedSignal = new SemaphoreSlim(0, 1);
        var sseContent = BuildSseContent(OpenCodeEventTypes.ServerConnected, "{}");

        // Use DelayedEndStream so the connection stays alive after delivering the event
        _sut = CreateService(new SseHttpMessageHandler(sseContent, delayBeforeResponse: TimeSpan.FromSeconds(30)));

        _sut.Subscribe(OpenCodeEventTypes.ServerConnected, _ =>
        {
            connectedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await connectedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        _sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_ReceivesEvent_NotifiesSubscriber()
    {
        // Arrange
        OpenCodeEventDto? receivedEvent = null;
        var receivedSignal = new SemaphoreSlim(0, 1);

        var sseContent = BuildSseContent("session.updated", """{"sessionId":"abc-123"}""");
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe("session.updated", dto =>
        {
            receivedEvent = dto;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.ShouldSatisfyAllConditions(
            () => receivedEvent.Type.ShouldBe("session.updated"),
            () => receivedEvent.Timestamp.ShouldBeGreaterThan(DateTimeOffset.MinValue),
            () => receivedEvent.Data.ShouldNotBeNull()
        );
    }

    [Fact]
    public async Task StartAsync_ReceivesEvent_NotifiesWildcardSubscriber()
    {
        // Arrange
        OpenCodeEventDto? receivedEvent = null;
        var receivedSignal = new SemaphoreSlim(0, 1);

        var sseContent = BuildSseContent("message.created", """{"text":"hello"}""");
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe(IOpenCodeEventService.AllEvents, dto =>
        {
            receivedEvent = dto;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Type.ShouldBe("message.created");
    }

    [Fact]
    public async Task StartAsync_ReceivesEvent_ParsesJsonData()
    {
        // Arrange
        OpenCodeEventDto? receivedEvent = null;
        var receivedSignal = new SemaphoreSlim(0, 1);

        var jsonPayload = """{"sessionId":"abc","count":42,"active":true}""";
        var sseContent = BuildSseContent("session.updated", jsonPayload);
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe("session.updated", dto =>
        {
            receivedEvent = dto;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Data.ShouldNotBeNull();
        receivedEvent.Data.Value.ValueKind.ShouldBe(JsonValueKind.Object);
        receivedEvent.Data.Value.GetProperty("sessionId").GetString().ShouldBe("abc");
        receivedEvent.Data.Value.GetProperty("count").GetInt32().ShouldBe(42);
        receivedEvent.Data.Value.GetProperty("active").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_ReceivesMalformedJsonData_SetsDataNull()
    {
        // Arrange
        OpenCodeEventDto? receivedEvent = null;
        var receivedSignal = new SemaphoreSlim(0, 1);

        var sseContent = BuildSseContent("session.updated", "not-valid-json{{{");
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe("session.updated", dto =>
        {
            receivedEvent = dto;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Type.ShouldBe("session.updated");
        receivedEvent.Data.ShouldBeNull();
    }

    [Fact]
    public async Task StartAsync_ReceivesEventWithNoData_SetsDataNull()
    {
        // Arrange
        OpenCodeEventDto? receivedEvent = null;
        var receivedSignal = new SemaphoreSlim(0, 1);

        // SSE event with event type but no data: line
        var sseContent = "event: session.updated\n\n";
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe("session.updated", dto =>
        {
            receivedEvent = dto;
            receivedSignal.Release();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Type.ShouldBe("session.updated");
        receivedEvent.Data.ShouldBeNull();
    }

    [Fact]
    public async Task StartAsync_MultipleSubscribers_AllNotified()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        var allReceivedSignal = new CountdownEvent(2);

        var sseContent = BuildSseContent("session.updated", """{"id":"1"}""");
        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        _sut.Subscribe("session.updated", _ =>
        {
            handler1Called = true;
            allReceivedSignal.Signal();
            return Task.CompletedTask;
        });

        _sut.Subscribe("session.updated", _ =>
        {
            handler2Called = true;
            allReceivedSignal.Signal();
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        allReceivedSignal.Wait(TimeSpan.FromSeconds(2));

        // Assert
        handler1Called.ShouldBeTrue();
        handler2Called.ShouldBeTrue();
    }

    // ── Consumer Error Handling ─────────────────────────────────────────

    [Fact]
    public async Task StartAsync_ConsumerThrowsException_DoesNotCrashStream()
    {
        // Arrange
        var secondHandlerEvents = new List<string>();
        var receivedSignal = new SemaphoreSlim(0, 1);

        // Two events in the SSE stream
        var sseContent =
            "event: session.updated\ndata: {\"id\":\"1\"}\n\n" +
            "event: message.created\ndata: {\"id\":\"2\"}\n\n";

        _sut = CreateService(new SseHttpMessageHandler(sseContent));

        // First subscriber: throws on every event
        _sut.Subscribe(IOpenCodeEventService.AllEvents, _ =>
            throw new InvalidOperationException("Consumer error!"));

        // Second subscriber: records events
        _sut.Subscribe(IOpenCodeEventService.AllEvents, dto =>
        {
            secondHandlerEvents.Add(dto.Type);
            if (dto.Type == "message.created")
            {
                receivedSignal.Release();
            }
            return Task.CompletedTask;
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await receivedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert — second handler should have received both events despite first handler throwing
        secondHandlerEvents.ShouldSatisfyAllConditions(
            () => secondHandlerEvents.ShouldContain("session.updated"),
            () => secondHandlerEvents.ShouldContain("message.created")
        );
    }

    // ── Start/Stop Lifecycle ────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_DoesNotStartAgain()
    {
        // Arrange — use a handler that blocks so the background task stays alive
        var connectedSignal = new SemaphoreSlim(0, 1);
        var sseContent = BuildSseContent(OpenCodeEventTypes.ServerConnected, "{}");
        var handler = new SseHttpMessageHandler(sseContent, delayBeforeResponse: TimeSpan.FromSeconds(30));
        _sut = CreateService(handler);

        _sut.Subscribe(OpenCodeEventTypes.ServerConnected, _ =>
        {
            connectedSignal.Release();
            return Task.CompletedTask;
        });

        await _sut.StartAsync(CancellationToken.None);

        // Wait for the background task to actually connect
        await connectedSignal.WaitAsync(TimeSpan.FromSeconds(2));

        // Act — calling StartAsync again should be idempotent (no exception, no second connection)
        await _sut.StartAsync(CancellationToken.None);

        // Assert — HTTP handler was only called once (the first start), not twice
        handler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));

        // Act & Assert — should not throw
        await Should.NotThrowAsync(
            async () => await _sut.StopAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));
        _sut.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(
            async () => await _sut.StartAsync(CancellationToken.None));

        // Prevent DisposeAsync from double-disposing
        _sut = null;
    }

    // ── Dispose ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_StopsBackgroundTask()
    {
        // Arrange
        var connectedSignal = new SemaphoreSlim(0, 1);
        var sseContent = BuildSseContent(OpenCodeEventTypes.ServerConnected, "{}");
        var handler = new SseHttpMessageHandler(sseContent, delayBeforeResponse: TimeSpan.FromSeconds(30));
        _sut = CreateService(handler);

        _sut.Subscribe(OpenCodeEventTypes.ServerConnected, _ =>
        {
            connectedSignal.Release();
            return Task.CompletedTask;
        });

        await _sut.StartAsync(CancellationToken.None);
        await connectedSignal.WaitAsync(TimeSpan.FromSeconds(2));
        _sut.IsConnected.ShouldBeTrue();

        // Act
        _sut.Dispose();

        // Assert
        _sut.IsConnected.ShouldBeFalse();

        // Prevent DisposeAsync from double-disposing
        _sut = null;
    }

    [Fact]
    public void Dispose_UnsubscribesFromServerManager()
    {
        // Arrange
        _sut = CreateService(new SseHttpMessageHandler(""));

        // Act
        _sut.Dispose();

        // Assert — fire the event; if still subscribed, it would try to start/stop
        // which would throw ObjectDisposedException. We verify no exception is thrown
        // by the server manager event, meaning the handler was unsubscribed.
        _serverManager.CurrentStatus.Returns(
            OpenCodeServerStatus.CreateRunning("1.0.0", OpenCodeConstants.DefaultServerUrl));

        // Raise the event — should not cause any side effects since we unsubscribed
        _serverManager.OnStatusChanged += Raise.Event<Action>();

        // If we get here without exception, the handler was properly unsubscribed
        // Prevent DisposeAsync from double-disposing
        _sut = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="OpenCodeEventService"/> with the specified HTTP message handler
    /// and the pre-configured mock dependencies.
    /// </summary>
    private OpenCodeEventService CreateService(HttpMessageHandler messageHandler)
    {
        var httpClient = new HttpClient(messageHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        return new OpenCodeEventService(
            httpClient,
            _scopeFactory,
            _serverManager,
            NullLogger<OpenCodeEventService>.Instance);
    }

    /// <summary>
    /// Creates a mock <see cref="IServiceScopeFactory"/> that resolves a substituted
    /// <see cref="IAppSettingService"/> from its scoped <see cref="IServiceProvider"/>.
    /// </summary>
    private static (IServiceScopeFactory scopeFactory, IAppSettingService appSettingService) CreateScopeFactory()
    {
        var appSettingService = Substitute.For<IAppSettingService>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IAppSettingService)).Returns(appSettingService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return (scopeFactory, appSettingService);
    }

    /// <summary>
    /// Builds a well-formed SSE content string with the given event type and optional JSON data.
    /// </summary>
    private static string BuildSseContent(string eventType, string? data = null)
    {
        var sb = new StringBuilder();
        sb.Append("event: ").Append(eventType).Append('\n');
        if (data is not null)
        {
            sb.Append("data: ").Append(data).Append('\n');
        }
        sb.Append('\n');
        return sb.ToString();
    }

    /// <summary>
    /// A custom <see cref="HttpMessageHandler"/> that returns a pre-built SSE response stream.
    /// The stream ends after the content is exhausted, causing the SSE reader to exit.
    /// Optionally delays before returning the response to simulate a long-lived connection.
    /// </summary>
    private sealed class SseHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _sseContent;
        private readonly TimeSpan _delayBeforeResponse;
        private int _callCount;

        /// <summary>
        /// Gets the number of times <see cref="SendAsync"/> was invoked.
        /// </summary>
        public int CallCount => _callCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SseHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="sseContent">The SSE-formatted content to return in the response body.</param>
        /// <param name="delayBeforeResponse">Optional delay before the stream ends, simulating a long-lived connection.</param>
        public SseHttpMessageHandler(string sseContent, TimeSpan delayBeforeResponse = default)
        {
            _sseContent = sseContent;
            _delayBeforeResponse = delayBeforeResponse;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);

            var contentBytes = Encoding.UTF8.GetBytes(_sseContent);
            Stream stream;

            if (_delayBeforeResponse > TimeSpan.Zero)
            {
                // Use a stream that provides the SSE content then blocks until cancelled
                stream = new DelayedEndStream(contentBytes, cancellationToken);
            }
            else
            {
                stream = new MemoryStream(contentBytes);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");

            await Task.CompletedTask;
            return response;
        }
    }

    /// <summary>
    /// A stream that returns the provided content bytes, then blocks on subsequent reads
    /// until the cancellation token is triggered. This simulates a long-lived SSE connection
    /// that delivers initial events and then stays open.
    /// </summary>
    private sealed class DelayedEndStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly CancellationToken _cancellationToken;

        public DelayedEndStream(byte[] content, CancellationToken cancellationToken)
        {
            _inner = new MemoryStream(content);
            _cancellationToken = cancellationToken;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            if (bytesRead > 0)
            {
                return bytesRead;
            }

            // Content exhausted — block until cancellation
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected — return 0 to signal end of stream
            }

            return 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _inner.ReadAsync(buffer, cancellationToken);
            if (bytesRead > 0)
            {
                return bytesRead;
            }

            // Content exhausted — block until cancellation
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected — return 0 to signal end of stream
            }

            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
