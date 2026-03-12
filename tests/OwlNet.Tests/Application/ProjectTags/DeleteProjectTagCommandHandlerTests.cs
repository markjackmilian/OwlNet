using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.ProjectTags.Commands.DeleteProjectTag;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.ProjectTags;

/// <summary>
/// Unit tests for <see cref="DeleteProjectTagCommandHandler"/>.
/// Covers tag-not-found, successful deletion, and repository interaction verification.
/// </summary>
public sealed class DeleteProjectTagCommandHandlerTests
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly DeleteProjectTagCommandHandler _sut;

    public DeleteProjectTagCommandHandlerTests()
    {
        _projectTagRepository = Substitute.For<IProjectTagRepository>();
        _sut = new DeleteProjectTagCommandHandler(
            _projectTagRepository,
            NullLogger<DeleteProjectTagCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Tag Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TagNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteProjectTagCommand { TagId = Guid.NewGuid() };

        _projectTagRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(result: null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_TagNotFound_DoesNotCallRemoveOrSave()
    {
        // Arrange
        var command = new DeleteProjectTagCommand { TagId = Guid.NewGuid() };

        _projectTagRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(result: null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _projectTagRepository.DidNotReceive().Remove(Arg.Any<ProjectTag>());
        await _projectTagRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidTagId_ReturnsSuccess()
    {
        // Arrange
        var tag = ProjectTag.Create("Bug", "#FF0000", Guid.NewGuid());
        var command = new DeleteProjectTagCommand { TagId = tag.Id };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidTagId_CallsRemoveAndSaveChanges()
    {
        // Arrange
        var tag = ProjectTag.Create("Bug", null, Guid.NewGuid());
        var command = new DeleteProjectTagCommand { TagId = tag.Id };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _projectTagRepository.Received(1).Remove(tag);
        await _projectTagRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
