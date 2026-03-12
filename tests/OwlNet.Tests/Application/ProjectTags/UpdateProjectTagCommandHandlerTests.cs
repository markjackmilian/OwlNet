using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.ProjectTags.Commands.UpdateProjectTag;
using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Application.ProjectTags;

/// <summary>
/// Unit tests for <see cref="UpdateProjectTagCommandHandler"/>.
/// Covers tag-not-found, duplicate-name guard, rename, color update, color clear,
/// and the no-rename-when-name-not-provided scenario.
/// </summary>
public sealed class UpdateProjectTagCommandHandlerTests
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly UpdateProjectTagCommandHandler _sut;

    public UpdateProjectTagCommandHandlerTests()
    {
        _projectTagRepository = Substitute.For<IProjectTagRepository>();
        _sut = new UpdateProjectTagCommandHandler(
            _projectTagRepository,
            NullLogger<UpdateProjectTagCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Tag Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TagNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateProjectTagCommand
        {
            TagId = Guid.NewGuid(),
            Name = "NewName"
        };

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
    public async Task Handle_TagNotFound_DoesNotSave()
    {
        // Arrange
        var command = new UpdateProjectTagCommand
        {
            TagId = Guid.NewGuid(),
            Name = "NewName"
        };

        _projectTagRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(result: null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _projectTagRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Duplicate Name Guard
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_RenameWithDuplicateName_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OriginalName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = "DuplicateName"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .ExistsByNameAsync(projectId, "DuplicateName", tag.Id, Arg.Any<CancellationToken>())
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
    public async Task Handle_RenameWithDuplicateName_DoesNotSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OriginalName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = "DuplicateName"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _projectTagRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Valid Rename
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRename_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OldName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = "NewName"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidRename_MutatesEntityNameAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OldName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = "NewName"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — entity was mutated in-place and SaveChanges was called
        tag.Name.ShouldBe("NewName");
        await _projectTagRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Color Update
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_UpdateColor_SetsNewColor()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("Tag", "#FF0000", projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Color = "#00FF00"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => tag.Color.ShouldBe("#00FF00")
        );
    }

    [Fact]
    public async Task Handle_ClearColor_SetsColorToNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("Tag", "#FF0000", projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            ClearColor = true
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => tag.Color.ShouldBeNull()
        );
    }

    [Fact]
    public async Task Handle_ClearColorTakesPrecedenceOverColor_SetsColorToNull()
    {
        // Arrange — both ClearColor=true and Color="#00FF00" provided; ClearColor wins
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("Tag", "#FF0000", projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Color = "#00FF00",
            ClearColor = true
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — ClearColor takes precedence
        tag.Color.ShouldBeNull();
    }

    // ──────────────────────────────────────────────
    // No-Op Scenarios
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoNameProvided_DoesNotRename()
    {
        // Arrange — Name is null, so no rename should occur
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OriginalName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = null,
            Color = "#AABBCC"
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — name unchanged, ExistsByName never called
        tag.Name.ShouldBe("OriginalName");
        await _projectTagRepository.DidNotReceive()
            .ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhitespaceNameProvided_DoesNotRename()
    {
        // Arrange — whitespace Name is treated as "not provided"
        var projectId = Guid.NewGuid();
        var tag = ProjectTag.Create("OriginalName", null, projectId);

        var command = new UpdateProjectTagCommand
        {
            TagId = tag.Id,
            Name = "   "
        };

        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _projectTagRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — name unchanged
        tag.Name.ShouldBe("OriginalName");
    }
}
