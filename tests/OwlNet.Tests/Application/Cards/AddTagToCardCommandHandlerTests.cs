using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.AddTagToCard;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Unit tests for <see cref="AddTagToCardCommandHandler"/>.
/// Covers card-not-found, tag-not-found, cross-project guard, happy path, and SaveChanges verification.
/// </summary>
public sealed class AddTagToCardCommandHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly IProjectTagRepository _projectTagRepository;
    private readonly AddTagToCardCommandHandler _sut;

    public AddTagToCardCommandHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _projectTagRepository = Substitute.For<IProjectTagRepository>();
        _sut = new AddTagToCardCommandHandler(
            _cardRepository,
            _projectTagRepository,
            NullLogger<AddTagToCardCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateCard(Guid projectId) =>
        Card.Create("Test Card", null, CardPriority.Medium, Guid.NewGuid(), projectId, 1, "user-123");

    // ──────────────────────────────────────────────
    // Card Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_CardNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new AddTagToCardCommand
        {
            CardId = Guid.NewGuid(),
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found")
        );
    }

    [Fact]
    public async Task Handle_CardNotFound_DoesNotQueryTagRepository()
    {
        // Arrange
        var command = new AddTagToCardCommand
        {
            CardId = Guid.NewGuid(),
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: tag repo must not be queried
        await _projectTagRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Tag Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TagNotFound_ReturnsFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
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
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(result: null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Cross-Project Guard
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_CrossProjectTag_ReturnsFailure()
    {
        // Arrange — card belongs to projectA, tag belongs to projectB
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var card = CreateCard(projectA);
        var tag = ProjectTag.Create("Bug", null, projectB);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tag.Id
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — domain rejected the cross-project assignment
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldNotBeNullOrWhiteSpace()
        );
    }

    [Fact]
    public async Task Handle_CrossProjectTag_DoesNotSave()
    {
        // Arrange
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var card = CreateCard(projectA);
        var tag = ProjectTag.Create("Bug", null, projectB);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tag.Id
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidAssignment_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tag = ProjectTag.Create("Feature", "#00FF00", projectId);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tag.Id
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidAssignment_CallsSaveChanges()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tag = ProjectTag.Create("Feature", null, projectId);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tag.Id
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidAssignment_AddsTagToCard()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tag = ProjectTag.Create("Feature", null, projectId);

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tag.Id
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _projectTagRepository
            .GetByIdAsync(tag.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(tag));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — tag was added to the card's domain collection
        card.Tags.ShouldContain(t => t.TagId == tag.Id);
    }

    // ──────────────────────────────────────────────
    // Idempotency: Tag Already Present (simulating EF Include)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TagAlreadyPresent_IsIdempotentAndReturnsSuccess()
    {
        // Arrange — simula card caricata da DB con Include: il tag è già nella collezione
        var projectId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var card = CreateCard(projectId);
        card.AddTag(tagId, projectId);   // tag pre-caricato, card.Tags.Count == 1

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tagId
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));

        // Il repository restituisce un ProjectTag con lo stesso ProjectId della card
        var projectTag = ProjectTag.Create("Existing", null, projectId);
        _projectTagRepository
            .GetByIdAsync(tagId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(projectTag));

        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — idempotente: successo, nessun duplicato, SaveChanges chiamato
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => card.Tags.Count.ShouldBe(1)
        );
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TagAlreadyPresent_DoesNotAddDuplicate()
    {
        // Arrange — card caricata da DB con il tag già presente nella collezione
        var projectId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var card = CreateCard(projectId);
        card.AddTag(tagId, projectId);   // tag pre-caricato

        var command = new AddTagToCardCommand
        {
            CardId = card.Id,
            TagId = tagId
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));

        var projectTag = ProjectTag.Create("Existing", null, projectId);
        _projectTagRepository
            .GetByIdAsync(tagId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ProjectTag?>(projectTag));

        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — la collezione non deve contenere duplicati
        card.Tags.Count.ShouldBe(1);
    }
}
