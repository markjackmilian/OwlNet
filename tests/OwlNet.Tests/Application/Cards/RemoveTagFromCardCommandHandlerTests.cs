using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.RemoveTagFromCard;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Unit tests for <see cref="RemoveTagFromCardCommandHandler"/>.
/// Covers card-not-found, no-op removal (tag not present), and successful removal.
/// </summary>
public sealed class RemoveTagFromCardCommandHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly RemoveTagFromCardCommandHandler _sut;

    public RemoveTagFromCardCommandHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _sut = new RemoveTagFromCardCommandHandler(
            _cardRepository,
            NullLogger<RemoveTagFromCardCommandHandler>.Instance);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateCard(Guid? projectId = null) =>
        Card.Create("Test Card", null, CardPriority.Medium, Guid.NewGuid(), projectId ?? Guid.NewGuid(), 1, "user-123");

    // ──────────────────────────────────────────────
    // Card Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_CardNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new RemoveTagFromCardCommand
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
    public async Task Handle_CardNotFound_DoesNotSave()
    {
        // Arrange
        var command = new RemoveTagFromCardCommand
        {
            CardId = Guid.NewGuid(),
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _cardRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // No-Op: Tag Not Present on Card
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCardTagNotPresent_ReturnsSuccess()
    {
        // Arrange — card exists but the tag was never assigned to it
        var card = CreateCard();

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = Guid.NewGuid()   // tag not on the card
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — no-op is still a success
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidCardTagNotPresent_StillCallsSaveChanges()
    {
        // Arrange
        var card = CreateCard();

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = Guid.NewGuid()
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — SaveChanges is always called when the card is found (no-op or not)
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Happy Path: Tag Present on Card
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCardWithTag_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();
        card.AddTag(tagId, projectId);

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = tagId
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidCardWithTag_RemovesTagFromCardAndSaves()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var card = CreateCard(projectId);
        var tagId = Guid.NewGuid();
        card.AddTag(tagId, projectId);

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = tagId
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — tag removed from domain collection and SaveChanges called
        card.Tags.ShouldBeEmpty();
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // Tag Pre-Caricato (simulando EF Include)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TagPresentInCollection_RemovesTagAndReturnsSuccess()
    {
        // Arrange — simula card caricata da DB con Include: il tag è già nella collezione
        var projectId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var card = CreateCard(projectId);
        card.AddTag(tagId, projectId);   // tag pre-caricato, card.Tags.Count == 1

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = tagId
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — tag rimosso dalla collezione, SaveChanges chiamato
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => card.Tags.Count.ShouldBe(0)
        );
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TagNotPresentInCollection_IsNoOpAndReturnsSuccess()
    {
        // Arrange — card senza tag: la rimozione è un no-op
        var card = CreateCard();

        var command = new RemoveTagFromCardCommand
        {
            CardId = card.Id,
            TagId = Guid.NewGuid()   // tag mai assegnato alla card
        };

        _cardRepository
            .GetEntityByIdWithTagsAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — no-op: successo, collezione ancora vuota, SaveChanges chiamato comunque
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => card.Tags.Count.ShouldBe(0)
        );
        await _cardRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
