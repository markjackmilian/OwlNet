using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.ProjectTags.Commands.CreateProjectTag;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.ProjectTags;

/// <summary>
/// Unit tests for <see cref="CreateProjectTagCommandHandler"/>.
/// Covers the happy path, duplicate-name guard, and repository interaction verification.
/// </summary>
public sealed class CreateProjectTagCommandHandlerTests
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly CreateProjectTagCommandHandler _sut;

    public CreateProjectTagCommandHandlerTests()
    {
        _projectTagRepository = Substitute.For<IProjectTagRepository>();
        _sut = new CreateProjectTagCommandHandler(
            _projectTagRepository,
            NullLogger<CreateProjectTagCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTagId()
    {
        // Arrange
        var command = new CreateProjectTagCommand
        {
            Name = "Bug",
            Color = "#FF5733",
            ProjectId = Guid.NewGuid()
        };

        _projectTagRepository
            .ExistsByNameAsync(command.ProjectId, command.Name, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
        _projectTagRepository
            .AddAsync(Arg.Any<ProjectTag>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsyncAndSaveChanges()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new CreateProjectTagCommand
        {
            Name = "Feature",
            Color = null,
            ProjectId = projectId
        };

        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
        _projectTagRepository
            .AddAsync(Arg.Any<ProjectTag>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _projectTagRepository.Received(1).AddAsync(
            Arg.Is<ProjectTag>(t => t.Name == "Feature" && t.ProjectId == projectId),
            Arg.Any<CancellationToken>());
        await _projectTagRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Duplicate Name Guard
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateTagName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateProjectTagCommand
        {
            Name = "Duplicate",
            ProjectId = Guid.NewGuid()
        };

        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public async Task Handle_DuplicateTagName_DoesNotCallAddAsyncOrSaveChanges()
    {
        // Arrange
        var command = new CreateProjectTagCommand
        {
            Name = "Duplicate",
            ProjectId = Guid.NewGuid()
        };

        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _projectTagRepository.DidNotReceive().AddAsync(Arg.Any<ProjectTag>(), Arg.Any<CancellationToken>());
        await _projectTagRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
