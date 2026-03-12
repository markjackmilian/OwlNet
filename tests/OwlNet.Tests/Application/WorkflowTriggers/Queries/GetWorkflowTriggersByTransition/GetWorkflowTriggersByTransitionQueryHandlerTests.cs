using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByTransition;
using Shouldly;

namespace OwlNet.Tests.Application.WorkflowTriggers.Queries.GetWorkflowTriggersByTransition;

/// <summary>
/// Unit tests for <see cref="GetWorkflowTriggersByTransitionQueryHandler"/>.
/// Verifies that the handler correctly delegates to the repository and returns
/// the matching enabled triggers for a given status transition.
/// </summary>
public sealed class GetWorkflowTriggersByTransitionQueryHandlerTests
{
    private readonly IWorkflowTriggerRepository _triggerRepository;
    private readonly GetWorkflowTriggersByTransitionQueryHandler _sut;

    public GetWorkflowTriggersByTransitionQueryHandlerTests()
    {
        _triggerRepository = Substitute.For<IWorkflowTriggerRepository>();
        _sut = new GetWorkflowTriggersByTransitionQueryHandler(
            _triggerRepository,
            NullLogger<GetWorkflowTriggersByTransitionQueryHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="WorkflowTriggerDto"/> with the supplied IDs and a single agent.
    /// </summary>
    private static WorkflowTriggerDto BuildTriggerDto(
        Guid triggerId,
        Guid projectId,
        Guid fromStatusId,
        Guid toStatusId) =>
        new(
            triggerId,
            projectId,
            "Code Review Trigger",
            fromStatusId,
            toStatusId,
            "Please review the code.",
            IsEnabled: true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [new WorkflowTriggerAgentDto(Guid.NewGuid(), triggerId, "code-reviewer", 0)]);

    // ──────────────────────────────────────────────
    // Happy Path — Matching Triggers Exist
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_MatchingTriggersExist_ReturnsTriggerList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();

        var trigger1 = BuildTriggerDto(Guid.NewGuid(), projectId, fromStatusId, toStatusId);
        var trigger2 = BuildTriggerDto(Guid.NewGuid(), projectId, fromStatusId, toStatusId);

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto> { trigger1, trigger2 }));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].Id.ShouldBe(trigger1.Id),
            () => result[0].ProjectId.ShouldBe(projectId),
            () => result[0].FromStatusId.ShouldBe(fromStatusId),
            () => result[0].ToStatusId.ShouldBe(toStatusId),
            () => result[1].Id.ShouldBe(trigger2.Id)
        );
    }

    [Fact]
    public async Task Handle_MatchingTriggersExist_EachTriggerHasAgents()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var trigger = BuildTriggerDto(triggerId, projectId, fromStatusId, toStatusId);

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto> { trigger }));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — the DTO's agent list is passed through unchanged
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(1),
            () => result[0].TriggerAgents.ShouldNotBeEmpty(),
            () => result[0].TriggerAgents[0].AgentName.ShouldBe("code-reviewer")
        );
    }

    // ──────────────────────────────────────────────
    // No Matching Triggers
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoMatchingTriggers_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto>()));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_NoMatchingTriggers_StillCallsRepository()
    {
        // Arrange — even when the result is empty, the repository must be queried.
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto>()));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _triggerRepository.Received(1).GetByTransitionAsync(
            projectId,
            fromStatusId,
            toStatusId,
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Repository Called With Correct Parameters
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_RepositoryCalledWithCorrectParameters()
    {
        // Arrange — verify the handler forwards all three IDs from the query to the repository
        // without modification.
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();

        _triggerRepository.GetByTransitionAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto>()));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert — exact IDs must be forwarded; no swapping or defaulting
        await _triggerRepository.Received(1).GetByTransitionAsync(
            Arg.Is<Guid>(id => id == projectId),
            Arg.Is<Guid>(id => id == fromStatusId),
            Arg.Is<Guid>(id => id == toStatusId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentTransitions_RepositoryCalledOncePerQuery()
    {
        // Arrange — two separate queries for different transitions must each result in
        // exactly one repository call with the correct IDs.
        var projectId = Guid.NewGuid();
        var fromA = Guid.NewGuid();
        var toA = Guid.NewGuid();
        var fromB = Guid.NewGuid();
        var toB = Guid.NewGuid();

        _triggerRepository.GetByTransitionAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<WorkflowTriggerDto>()));

        var queryA = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromA,
            ToStatusId = toA
        };

        var queryB = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromB,
            ToStatusId = toB
        };

        // Act
        await _sut.Handle(queryA, CancellationToken.None);
        await _sut.Handle(queryB, CancellationToken.None);

        // Assert — each transition was queried independently with its own IDs
        await _triggerRepository.Received(1).GetByTransitionAsync(
            projectId, fromA, toA, Arg.Any<CancellationToken>());
        await _triggerRepository.Received(1).GetByTransitionAsync(
            projectId, fromB, toB, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Return Value Integrity
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_RepositoryReturnsTriggers_ReturnsExactRepositoryResult()
    {
        // Arrange — the handler must return the repository's list verbatim (no filtering,
        // no mapping, no reordering).
        var projectId = Guid.NewGuid();
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();

        var repositoryResult = new List<WorkflowTriggerDto>
        {
            BuildTriggerDto(Guid.NewGuid(), projectId, fromStatusId, toStatusId),
            BuildTriggerDto(Guid.NewGuid(), projectId, fromStatusId, toStatusId),
            BuildTriggerDto(Guid.NewGuid(), projectId, fromStatusId, toStatusId)
        };

        _triggerRepository.GetByTransitionAsync(
                projectId, fromStatusId, toStatusId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(repositoryResult));

        var query = new GetWorkflowTriggersByTransitionQuery
        {
            ProjectId = projectId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId
        };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — same reference and same count; handler does not wrap or transform
        result.ShouldBeSameAs(repositoryResult);
        result.Count.ShouldBe(3);
    }
}
