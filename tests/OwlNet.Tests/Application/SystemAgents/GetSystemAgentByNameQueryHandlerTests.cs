using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.SystemAgents.Queries.GetSystemAgentByName;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.SystemAgents;

/// <summary>
/// Unit tests for <see cref="GetSystemAgentByNameQueryHandler"/>.
/// Covers the happy path (agent found), the not-found path, and the
/// repository interaction contract (correct name forwarded to the repository).
/// </summary>
public sealed class GetSystemAgentByNameQueryHandlerTests
{
    private readonly ISystemAgentRepository _repository;
    private readonly GetSystemAgentByNameQueryHandler _sut;

    public GetSystemAgentByNameQueryHandlerTests()
    {
        _repository = Substitute.For<ISystemAgentRepository>();
        _sut = new GetSystemAgentByNameQueryHandler(
            _repository,
            NullLogger<GetSystemAgentByNameQueryHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Helper — factory for valid SystemAgent instances
    // ──────────────────────────────────────────────

    private static SystemAgent CreateAgent(
        string name = "test-agent",
        string displayName = "Test Agent",
        string description = "A test agent for unit testing purposes",
        string mode = "primary",
        string content = "# Test Agent\nThis is test content.") =>
        SystemAgent.Create(name, displayName, description, mode, content);

    // ──────────────────────────────────────────────
    // Happy Path — Agent found by name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingName_ReturnsSuccessWithAllDtoFieldsMapped()
    {
        // Arrange
        var agent = CreateAgent(
            name: "owl-coder",
            displayName: "Owl Coder",
            description: "A test agent for unit testing purposes",
            mode: "subagent",
            content: "# Owl Coder\nContent here.");

        _repository.GetByNameAsync("owl-coder", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));

        var query = new GetSystemAgentByNameQuery { Name = "owl-coder" };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Id.ShouldBe(agent.Id),
            () => result.Value.Name.ShouldBe("owl-coder"),
            () => result.Value.DisplayName.ShouldBe("Owl Coder"),
            () => result.Value.Description.ShouldBe("A test agent for unit testing purposes"),
            () => result.Value.Mode.ShouldBe("subagent"),
            () => result.Value.Content.ShouldBe("# Owl Coder\nContent here."),
            () => result.Value.CreatedAt.ShouldBe(agent.CreatedAt),
            () => result.Value.UpdatedAt.ShouldBe(agent.UpdatedAt)
        );
    }

    // ──────────────────────────────────────────────
    // Not Found — Name not in repository
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NonExistingName_ReturnsFailureWithExpectedMessage()
    {
        // Arrange
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var query = new GetSystemAgentByNameQuery { Name = "ghost-agent" };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("System agent not found.")
        );
    }

    // ──────────────────────────────────────────────
    // Repository Contract — Correct name forwarded
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AnyName_CallsGetByNameAsyncWithExactName()
    {
        // Arrange
        var agent = CreateAgent(name: "git-agent");

        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));

        var query = new GetSystemAgentByNameQuery { Name = "git-agent" };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByNameAsync(
            Arg.Is<string>(n => n == "git-agent"),
            Arg.Any<CancellationToken>());
    }
}
