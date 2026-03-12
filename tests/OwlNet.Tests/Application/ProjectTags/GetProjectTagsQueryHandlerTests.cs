using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Application.ProjectTags.Queries.GetProjectTags;
using Shouldly;

namespace OwlNet.Tests.Application.ProjectTags;

/// <summary>
/// Unit tests for <see cref="GetProjectTagsQueryHandler"/>.
/// Covers the happy path (tags returned), empty project, and repository call verification.
/// </summary>
public sealed class GetProjectTagsQueryHandlerTests
{
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly GetProjectTagsQueryHandler _sut;

    public GetProjectTagsQueryHandlerTests()
    {
        _projectTagRepository = Substitute.For<IProjectTagRepository>();
        _sut = new GetProjectTagsQueryHandler(
            _projectTagRepository,
            NullLogger<GetProjectTagsQueryHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectWithTags_ReturnsTagsOrderedByName()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Repository returns tags already ordered by name (as the real implementation does)
        var expectedTags = new List<ProjectTagDto>
        {
            new(Guid.NewGuid(), projectId, "Alpha",   "#FF0000", now, now),
            new(Guid.NewGuid(), projectId, "Beta",    null,      now, now),
            new(Guid.NewGuid(), projectId, "Gamma",   "#00FF00", now, now)
        };

        _projectTagRepository
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IReadOnlyList<ProjectTagDto>>(expectedTags));

        var query = new GetProjectTagsQuery { ProjectId = projectId };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(3),
            () => result[0].Name.ShouldBe("Alpha"),
            () => result[1].Name.ShouldBe("Beta"),
            () => result[2].Name.ShouldBe("Gamma")
        );
    }

    [Fact]
    public async Task Handle_ProjectWithTags_CallsRepositoryWithCorrectProjectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectTagRepository
            .GetByProjectIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IReadOnlyList<ProjectTagDto>>(new List<ProjectTagDto>()));

        var query = new GetProjectTagsQuery { ProjectId = projectId };

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _projectTagRepository.Received(1)
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Empty Project
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectWithNoTags_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _projectTagRepository
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IReadOnlyList<ProjectTagDto>>(new List<ProjectTagDto>()));

        var query = new GetProjectTagsQuery { ProjectId = projectId };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────
    // DTO Mapping
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProjectWithTags_ReturnsDtosWithCorrectProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var expectedTags = new List<ProjectTagDto>
        {
            new(tagId, projectId, "MyTag", "#AABBCC", now.AddDays(-1), now)
        };

        _projectTagRepository
            .GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IReadOnlyList<ProjectTagDto>>(expectedTags));

        var query = new GetProjectTagsQuery { ProjectId = projectId };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var dto = result[0];
        dto.ShouldSatisfyAllConditions(
            () => dto.Id.ShouldBe(tagId),
            () => dto.ProjectId.ShouldBe(projectId),
            () => dto.Name.ShouldBe("MyTag"),
            () => dto.Color.ShouldBe("#AABBCC")
        );
    }
}
