using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.SystemAgents.Commands.CreateSystemAgent;
using OwlNet.Application.SystemAgents.Commands.DeleteSystemAgent;
using OwlNet.Application.SystemAgents.Commands.UpdateSystemAgent;
using OwlNet.Application.SystemAgents.Queries.GetAllSystemAgents;
using OwlNet.Application.SystemAgents.Queries.GetSystemAgentById;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.SystemAgents;

/// <summary>
/// Comprehensive unit tests for all SystemAgent CQRS command and query handlers.
/// Covers <see cref="GetAllSystemAgentsQueryHandler"/>, <see cref="GetSystemAgentByIdQueryHandler"/>,
/// <see cref="CreateSystemAgentCommandHandler"/>, <see cref="UpdateSystemAgentCommandHandler"/>,
/// and <see cref="DeleteSystemAgentCommandHandler"/>.
/// Each handler is tested for its happy path, not-found scenarios, and duplicate-name guard.
/// </summary>
public sealed class SystemAgentHandlerTests
{
    private readonly ISystemAgentRepository _repository;

    public SystemAgentHandlerTests()
    {
        _repository = Substitute.For<ISystemAgentRepository>();
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
    // GetAllSystemAgentsQueryHandler — With agents
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllSystemAgents_WithAgents_ReturnsSuccessWithSortedDtos()
    {
        // Arrange
        var agent1 = CreateAgent(name: "alpha-agent", displayName: "Alpha Agent");
        var agent2 = CreateAgent(name: "beta-agent",  displayName: "Beta Agent");
        var agent3 = CreateAgent(name: "gamma-agent", displayName: "Gamma Agent");

        var agents = new List<SystemAgent> { agent1, agent2, agent3 }.AsReadOnly();

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SystemAgent>>(agents));

        var sut = new GetAllSystemAgentsQueryHandler(
            _repository,
            NullLogger<GetAllSystemAgentsQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(new GetAllSystemAgentsQuery(), CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Count.ShouldBe(3),
            () => result.Value[0].Name.ShouldBe("alpha-agent"),
            () => result.Value[1].Name.ShouldBe("beta-agent"),
            () => result.Value[2].Name.ShouldBe("gamma-agent")
        );
    }

    // ──────────────────────────────────────────────
    // GetAllSystemAgentsQueryHandler — Empty repository
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllSystemAgents_EmptyRepository_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SystemAgent>>(new List<SystemAgent>().AsReadOnly()));

        var sut = new GetAllSystemAgentsQueryHandler(
            _repository,
            NullLogger<GetAllSystemAgentsQueryHandler>.Instance);

        // Act
        var result = await sut.Handle(new GetAllSystemAgentsQuery(), CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // GetSystemAgentByIdQueryHandler — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSystemAgentById_ExistingId_ReturnsSuccessWithDto()
    {
        // Arrange
        var agent = CreateAgent(
            name: "my-agent",
            displayName: "My Agent",
            mode: "subagent");

        _repository.GetByIdAsync(agent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));

        var sut = new GetSystemAgentByIdQueryHandler(
            _repository,
            NullLogger<GetSystemAgentByIdQueryHandler>.Instance);

        var query = new GetSystemAgentByIdQuery { Id = agent.Id };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.Id.ShouldBe(agent.Id),
            () => result.Value.Name.ShouldBe("my-agent"),
            () => result.Value.DisplayName.ShouldBe("My Agent"),
            () => result.Value.Mode.ShouldBe("subagent")
        );
    }

    // ──────────────────────────────────────────────
    // GetSystemAgentByIdQueryHandler — Not found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetSystemAgentById_NonExistingId_ReturnsFailure()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var sut = new GetSystemAgentByIdQueryHandler(
            _repository,
            NullLogger<GetSystemAgentByIdQueryHandler>.Instance);

        var query = new GetSystemAgentByIdQuery { Id = unknownId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("System agent not found.")
        );
    }

    // ──────────────────────────────────────────────
    // CreateSystemAgentCommandHandler — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSystemAgent_ValidCommand_ReturnsSuccessWithId()
    {
        // Arrange
        var command = new CreateSystemAgentCommand
        {
            Name        = "new-agent",
            DisplayName = "New Agent",
            Description = "A brand new system agent for testing",
            Mode        = "primary",
            Content     = "# New Agent\nContent here."
        };

        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));
        _repository.AddAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateSystemAgentCommandHandler(
            _repository,
            NullLogger<CreateSystemAgentCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task CreateSystemAgent_ValidCommand_PersistsAgentToRepository()
    {
        // Arrange
        var command = new CreateSystemAgentCommand
        {
            Name        = "persist-agent",
            DisplayName = "Persist Agent",
            Description = "Agent that should be persisted to the repository",
            Mode        = "all",
            Content     = "# Persist Agent\nContent."
        };

        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));
        _repository.AddAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CreateSystemAgentCommandHandler(
            _repository,
            NullLogger<CreateSystemAgentCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<SystemAgent>(a =>
                a.Name        == "persist-agent" &&
                a.DisplayName == "Persist Agent" &&
                a.Mode        == "all"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // CreateSystemAgentCommandHandler — Duplicate name
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateSystemAgent_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var existingAgent = CreateAgent(name: "existing-agent");

        var command = new CreateSystemAgentCommand
        {
            Name        = "existing-agent",
            DisplayName = "Existing Agent",
            Description = "This name is already taken by another agent",
            Mode        = "primary",
            Content     = "# Existing Agent\nContent."
        };

        _repository.GetByNameAsync("existing-agent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(existingAgent));

        var sut = new CreateSystemAgentCommandHandler(
            _repository,
            NullLogger<CreateSystemAgentCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("A system agent with this name already exists.")
        );
    }

    [Fact]
    public async Task CreateSystemAgent_DuplicateName_DoesNotPersist()
    {
        // Arrange
        var existingAgent = CreateAgent(name: "duplicate-agent");

        var command = new CreateSystemAgentCommand
        {
            Name        = "duplicate-agent",
            DisplayName = "Duplicate Agent",
            Description = "This name is already taken by another agent",
            Mode        = "primary",
            Content     = "# Duplicate Agent\nContent."
        };

        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(existingAgent));

        var sut = new CreateSystemAgentCommandHandler(
            _repository,
            NullLogger<CreateSystemAgentCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateSystemAgentCommandHandler — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateSystemAgent_ExistingAgent_ReturnsSuccess()
    {
        // Arrange
        var agent = CreateAgent(name: "update-agent", displayName: "Original Display");

        var command = new UpdateSystemAgentCommand
        {
            Id          = agent.Id,
            DisplayName = "Updated Display",
            Description = "Updated description for the system agent",
            Mode        = "subagent",
            Content     = "# Updated Agent\nNew content."
        };

        _repository.GetByIdAsync(agent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));
        _repository.UpdateAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new UpdateSystemAgentCommandHandler(
            _repository,
            NullLogger<UpdateSystemAgentCommandHandler>.Instance);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateSystemAgent_ExistingAgent_CallsUpdateAsync()
    {
        // Arrange
        var agent = CreateAgent(name: "update-agent", displayName: "Original Display");

        var command = new UpdateSystemAgentCommand
        {
            Id          = agent.Id,
            DisplayName = "Updated Display",
            Description = "Updated description for the system agent",
            Mode        = "subagent",
            Content     = "# Updated Agent\nNew content."
        };

        _repository.GetByIdAsync(agent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));
        _repository.UpdateAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new UpdateSystemAgentCommandHandler(
            _repository,
            NullLogger<UpdateSystemAgentCommandHandler>.Instance);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).UpdateAsync(
            Arg.Is<SystemAgent>(a =>
                a.Id          == agent.Id &&
                a.DisplayName == "Updated Display" &&
                a.Mode        == "subagent"),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // UpdateSystemAgentCommandHandler — Not found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateSystemAgent_NonExistingId_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var sut = new UpdateSystemAgentCommandHandler(
            _repository,
            NullLogger<UpdateSystemAgentCommandHandler>.Instance);

        var command = new UpdateSystemAgentCommand
        {
            Id          = Guid.NewGuid(),
            DisplayName = "Ghost Agent",
            Description = "This agent does not exist in the repository",
            Mode        = "primary",
            Content     = "# Ghost\nContent."
        };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("System agent not found.")
        );
    }

    [Fact]
    public async Task UpdateSystemAgent_NonExistingId_DoesNotCallUpdateAsync()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var sut = new UpdateSystemAgentCommandHandler(
            _repository,
            NullLogger<UpdateSystemAgentCommandHandler>.Instance);

        var command = new UpdateSystemAgentCommand
        {
            Id          = Guid.NewGuid(),
            DisplayName = "Ghost Agent",
            Description = "This agent does not exist in the repository",
            Mode        = "primary",
            Content     = "# Ghost\nContent."
        };

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteSystemAgentCommandHandler — Happy path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteSystemAgent_ExistingAgent_ReturnsSuccess()
    {
        // Arrange
        var agent = CreateAgent(name: "delete-agent");

        _repository.GetByIdAsync(agent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));
        _repository.DeleteAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DeleteSystemAgentCommandHandler(
            _repository,
            NullLogger<DeleteSystemAgentCommandHandler>.Instance);

        var command = new DeleteSystemAgentCommand { Id = agent.Id };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSystemAgent_ExistingAgent_CallsDeleteAsync()
    {
        // Arrange
        var agent = CreateAgent(name: "delete-agent");

        _repository.GetByIdAsync(agent.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(agent));
        _repository.DeleteAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new DeleteSystemAgentCommandHandler(
            _repository,
            NullLogger<DeleteSystemAgentCommandHandler>.Instance);

        var command = new DeleteSystemAgentCommand { Id = agent.Id };

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).DeleteAsync(
            Arg.Is<SystemAgent>(a => a.Id == agent.Id),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteSystemAgentCommandHandler — Not found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteSystemAgent_NonExistingId_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var sut = new DeleteSystemAgentCommandHandler(
            _repository,
            NullLogger<DeleteSystemAgentCommandHandler>.Instance);

        var command = new DeleteSystemAgentCommand { Id = Guid.NewGuid() };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldBe("System agent not found.")
        );
    }

    [Fact]
    public async Task DeleteSystemAgent_NonExistingId_DoesNotCallDeleteAsync()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SystemAgent?>(null));

        var sut = new DeleteSystemAgentCommandHandler(
            _repository,
            NullLogger<DeleteSystemAgentCommandHandler>.Instance);

        var command = new DeleteSystemAgentCommand { Id = Guid.NewGuid() };

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<SystemAgent>(), Arg.Any<CancellationToken>());
    }
}
