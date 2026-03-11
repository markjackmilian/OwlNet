using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.WorkflowTriggers.Commands.CreateWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Commands.DeleteWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Commands.UpdateWorkflowTrigger;
using OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByProject;
using OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByTransition;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.WorkflowTriggers;

/// <summary>
/// Comprehensive unit tests for all WorkflowTrigger CQRS command and query handlers.
/// Covers <see cref="CreateWorkflowTriggerCommandHandler"/>,
/// <see cref="UpdateWorkflowTriggerCommandHandler"/>,
/// <see cref="DeleteWorkflowTriggerCommandHandler"/>,
/// <see cref="GetWorkflowTriggersByProjectQueryHandler"/>, and
/// <see cref="GetWorkflowTriggersByTransitionQueryHandler"/>.
/// Each handler is tested for its happy path, validation failures, edge cases, and error scenarios.
/// </summary>
public sealed class WorkflowTriggerHandlerTests
{
    private readonly IWorkflowTriggerRepository _triggerRepository;
    private readonly IBoardStatusRepository _boardStatusRepository;

    public WorkflowTriggerHandlerTests()
    {
        _triggerRepository = Substitute.For<IWorkflowTriggerRepository>();
        _boardStatusRepository = Substitute.For<IBoardStatusRepository>();

        // Safe defaults: persist operations succeed silently.
        _triggerRepository.AddAsync(Arg.Any<WorkflowTrigger>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _triggerRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds a valid <see cref="WorkflowTriggerDto"/> for the given IDs.
    /// </summary>
    private static WorkflowTriggerDto BuildTriggerDto(
        Guid triggerId, Guid projectId, Guid fromStatusId, Guid toStatusId) =>
        new(triggerId, projectId, "My Trigger", fromStatusId, toStatusId,
            "Do a code review", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            [new WorkflowTriggerAgentDto(Guid.NewGuid(), triggerId, "code-reviewer", 0)]);

    /// <summary>Creates a <see cref="CreateWorkflowTriggerCommandHandler"/> with the shared substitutes.</summary>
    private CreateWorkflowTriggerCommandHandler CreateHandler() =>
        new(_triggerRepository, _boardStatusRepository,
            NullLogger<CreateWorkflowTriggerCommandHandler>.Instance);

    /// <summary>Creates an <see cref="UpdateWorkflowTriggerCommandHandler"/> with the shared substitutes.</summary>
    private UpdateWorkflowTriggerCommandHandler UpdateHandler() =>
        new(_triggerRepository, _boardStatusRepository,
            NullLogger<UpdateWorkflowTriggerCommandHandler>.Instance);

    // ──────────────────────────────────────────────
    // CreateWorkflowTriggerCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTrigger_ValidCommand_ReturnsSuccessWithId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);

        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(toStatus));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Code Review Trigger",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatus.Id,
            Prompt = "Please review the code.",
            Agents = [new CreateWorkflowTriggerAgentItem("code-reviewer", 0)]
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task CreateTrigger_ValidCommand_PersistsTriggerWithAgents()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);

        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(toStatus));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Code Review Trigger",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatus.Id,
            Prompt = "Please review the code.",
            Agents = [new CreateWorkflowTriggerAgentItem("code-reviewer", 0)]
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — trigger persisted with correct project and one agent
        await _triggerRepository.Received(1).AddAsync(
            Arg.Is<WorkflowTrigger>(t =>
                t.ProjectId == projectId &&
                t.FromStatusId == fromStatus.Id &&
                t.ToStatusId == toStatus.Id &&
                t.TriggerAgents.Count == 1 &&
                t.TriggerAgents[0].AgentName == "code-reviewer"),
            Arg.Any<CancellationToken>());
        await _triggerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // CreateWorkflowTriggerCommand — FromStatus Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTrigger_FromStatusNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();

        _boardStatusRepository.GetEntityByIdAsync(fromStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Trigger",
            FromStatusId = fromStatusId,
            ToStatusId = Guid.NewGuid(),
            Prompt = "Do something.",
            Agents = [new CreateWorkflowTriggerAgentItem("agent", 0)]
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Source status not found.")
        );
    }

    // ──────────────────────────────────────────────
    // CreateWorkflowTriggerCommand — FromStatus Belongs to Different Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTrigger_FromStatusBelongsToDifferentProject_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();

        // fromStatus belongs to a different project
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, otherProjectId);

        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Trigger",
            FromStatusId = fromStatus.Id,
            ToStatusId = Guid.NewGuid(),
            Prompt = "Do something.",
            Agents = [new CreateWorkflowTriggerAgentItem("agent", 0)]
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Source status does not belong to this project.")
        );
    }

    // ──────────────────────────────────────────────
    // CreateWorkflowTriggerCommand — ToStatus Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTrigger_ToStatusNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatusId = Guid.NewGuid();

        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(null));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Trigger",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatusId,
            Prompt = "Do something.",
            Agents = [new CreateWorkflowTriggerAgentItem("agent", 0)]
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Destination status not found.")
        );
    }

    // ──────────────────────────────────────────────
    // CreateWorkflowTriggerCommand — ToStatus Belongs to Different Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTrigger_ToStatusBelongsToDifferentProject_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);

        // toStatus belongs to a different project
        var toStatus = BoardStatus.CreateForProject("Review", 1, otherProjectId);

        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(toStatus));

        var command = new CreateWorkflowTriggerCommand
        {
            ProjectId = projectId,
            Name = "Trigger",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatus.Id,
            Prompt = "Do something.",
            Agents = [new CreateWorkflowTriggerAgentItem("agent", 0)]
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Destination status does not belong to this project.")
        );
    }

    // ──────────────────────────────────────────────
    // UpdateWorkflowTriggerCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTrigger_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);
        var trigger = WorkflowTrigger.Create(
            projectId, "Old Name", fromStatus.Id, toStatus.Id, "Old prompt.");

        _triggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));
        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(toStatus));

        var command = new UpdateWorkflowTriggerCommand
        {
            TriggerId = trigger.Id,
            Name = "Updated Name",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatus.Id,
            Prompt = "Updated prompt.",
            IsEnabled = true,
            Agents = [new UpdateWorkflowTriggerAgentItem("code-reviewer", 0)]
        };

        // Act
        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => trigger.Name.ShouldBe("Updated Name"),
            () => trigger.Prompt.ShouldBe("Updated prompt.")
        );
        await _triggerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTrigger_ValidCommand_RemovesOldAgentsBeforeSettingNew()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);
        var trigger = WorkflowTrigger.Create(
            projectId, "My Trigger", fromStatus.Id, toStatus.Id, "Original prompt.");

        // Seed the trigger with an existing agent so TriggerAgents is non-empty.
        var existingAgent = WorkflowTriggerAgent.Create(trigger.Id, "old-agent", 0);
        trigger.SetAgents([existingAgent]);

        _triggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));
        _boardStatusRepository.GetEntityByIdAsync(fromStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(fromStatus));
        _boardStatusRepository.GetEntityByIdAsync(toStatus.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BoardStatus?>(toStatus));

        var command = new UpdateWorkflowTriggerCommand
        {
            TriggerId = trigger.Id,
            Name = "Updated Name",
            FromStatusId = fromStatus.Id,
            ToStatusId = toStatus.Id,
            Prompt = "Updated prompt.",
            IsEnabled = true,
            Agents = [new UpdateWorkflowTriggerAgentItem("new-agent", 0)]
        };

        // Act
        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        // Assert — RemoveAgents was called with the old agent collection before SetAgents replaced it.
        // We use Arg.Is with a predicate that checks the agent name at call time (NSubstitute
        // evaluates the predicate against the argument snapshot it recorded during the call).
        result.IsSuccess.ShouldBeTrue();
        _triggerRepository.Received(1).RemoveAgents(
            Arg.Is<IEnumerable<WorkflowTriggerAgent>>(
                agents => agents.Any(a => a.AgentName == "old-agent")));
    }

    // ──────────────────────────────────────────────
    // UpdateWorkflowTriggerCommand — Trigger Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTrigger_TriggerNotFound_ReturnsFailure()
    {
        // Arrange
        var triggerId = Guid.NewGuid();

        _triggerRepository.GetEntityByIdAsync(triggerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(null));

        var command = new UpdateWorkflowTriggerCommand
        {
            TriggerId = triggerId,
            Name = "Updated Name",
            FromStatusId = Guid.NewGuid(),
            ToStatusId = Guid.NewGuid(),
            Prompt = "Updated prompt.",
            IsEnabled = true,
            Agents = [new UpdateWorkflowTriggerAgentItem("agent", 0)]
        };

        // Act
        var result = await UpdateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Workflow trigger not found.")
        );
    }

    // ──────────────────────────────────────────────
    // DeleteWorkflowTriggerCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTrigger_ExistingTrigger_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);
        var trigger = WorkflowTrigger.Create(
            projectId, "My Trigger", fromStatus.Id, toStatus.Id, "Do a code review.");

        _triggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));

        var sut = new DeleteWorkflowTriggerCommandHandler(
            _triggerRepository,
            NullLogger<DeleteWorkflowTriggerCommandHandler>.Instance);

        var command = new DeleteWorkflowTriggerCommand { TriggerId = trigger.Id };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteTrigger_ExistingTrigger_RemovesAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatus = BoardStatus.CreateForProject("In Progress", 0, projectId);
        var toStatus = BoardStatus.CreateForProject("Review", 1, projectId);
        var trigger = WorkflowTrigger.Create(
            projectId, "My Trigger", fromStatus.Id, toStatus.Id, "Do a code review.");

        _triggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));

        var sut = new DeleteWorkflowTriggerCommandHandler(
            _triggerRepository,
            NullLogger<DeleteWorkflowTriggerCommandHandler>.Instance);

        var command = new DeleteWorkflowTriggerCommand { TriggerId = trigger.Id };

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _triggerRepository.Received(1).Remove(trigger);
        await _triggerRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // DeleteWorkflowTriggerCommand — Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTrigger_TriggerNotFound_ReturnsFailure()
    {
        // Arrange
        var triggerId = Guid.NewGuid();

        _triggerRepository.GetEntityByIdAsync(triggerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(null));

        var sut = new DeleteWorkflowTriggerCommandHandler(
            _triggerRepository,
            NullLogger<DeleteWorkflowTriggerCommandHandler>.Instance);

        var command = new DeleteWorkflowTriggerCommand { TriggerId = triggerId };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Error.ShouldBe("Workflow trigger not found.")
        );
    }

    [Fact]
    public async Task DeleteTrigger_TriggerNotFound_DoesNotCallRemove()
    {
        // Arrange
        var triggerId = Guid.NewGuid();

        _triggerRepository.GetEntityByIdAsync(triggerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(null));

        var sut = new DeleteWorkflowTriggerCommandHandler(
            _triggerRepository,
            NullLogger<DeleteWorkflowTriggerCommandHandler>.Instance);

        var command = new DeleteWorkflowTriggerCommand { TriggerId = triggerId };

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _triggerRepository.DidNotReceive().Remove(Arg.Any<WorkflowTrigger>());
        await _triggerRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetWorkflowTriggersByProjectQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetByProject_ReturnsRepositoryResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var expectedTriggers = new List<WorkflowTriggerDto>
        {
            BuildTriggerDto(triggerId, projectId, fromStatusId, toStatusId)
        };

        _triggerRepository.GetByProjectIdAsync(projectId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedTriggers));

        var sut = new GetWorkflowTriggersByProjectQueryHandler(
            _triggerRepository,
            NullLogger<GetWorkflowTriggersByProjectQueryHandler>.Instance);

        var query = new GetWorkflowTriggersByProjectQuery { ProjectId = projectId };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(1),
            () => result[0].Id.ShouldBe(triggerId),
            () => result[0].ProjectId.ShouldBe(projectId)
        );
    }

    [Fact]
    public async Task GetByProject_WithIsEnabledFilter_PassesFilterToRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _triggerRepository.GetByProjectIdAsync(projectId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto>()));

        var sut = new GetWorkflowTriggersByProjectQueryHandler(
            _triggerRepository,
            NullLogger<GetWorkflowTriggersByProjectQueryHandler>.Instance);

        var query = new GetWorkflowTriggersByProjectQuery
        {
            ProjectId = projectId,
            IsEnabled = true
        };

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert — the handler must forward the IsEnabled filter to the repository
        await _triggerRepository.Received(1).GetByProjectIdAsync(
            projectId,
            true,
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetWorkflowTriggersByTransitionQuery — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetByTransition_ReturnsRepositoryResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var expectedTriggers = new List<WorkflowTriggerDto>
        {
            BuildTriggerDto(triggerId, projectId, fromStatusId, toStatusId)
        };

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedTriggers));

        var sut = new GetWorkflowTriggersByTransitionQueryHandler(
            _triggerRepository,
            NullLogger<GetWorkflowTriggersByTransitionQueryHandler>.Instance);

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(1),
            () => result[0].Id.ShouldBe(triggerId),
            () => result[0].FromStatusId.ShouldBe(fromStatusId),
            () => result[0].ToStatusId.ShouldBe(toStatusId)
        );
    }
}
