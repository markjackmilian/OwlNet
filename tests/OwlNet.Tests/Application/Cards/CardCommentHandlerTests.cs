using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.AddAgentComment;
using OwlNet.Application.Cards.Commands.AddHumanComment;
using OwlNet.Application.Cards.Queries.GetCardComments;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Unit tests for the Card Comment CQRS handlers:
/// <see cref="AddHumanCommentCommandHandler"/>, <see cref="AddAgentCommentCommandHandler"/>,
/// and <see cref="GetCardCommentsQueryHandler"/>.
/// Each handler is tested for its happy path, repository interaction verification,
/// card-not-found failure, and the resulting guard against unnecessary persistence calls.
/// </summary>
public sealed class CardCommentHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly ICardCommentRepository _cardCommentRepository;

    public CardCommentHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _cardCommentRepository = Substitute.For<ICardCommentRepository>();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static Card CreateValidCard() =>
        Card.Create(
            title: "Test Card",
            description: null,
            priority: CardPriority.Medium,
            statusId: Guid.NewGuid(),
            projectId: Guid.NewGuid(),
            number: 1,
            createdBy: "user-123");

    private AddHumanCommentCommandHandler BuildHumanHandler() =>
        new(_cardRepository, _cardCommentRepository,
            NullLogger<AddHumanCommentCommandHandler>.Instance);

    private AddAgentCommentCommandHandler BuildAgentHandler() =>
        new(_cardRepository, _cardCommentRepository,
            NullLogger<AddAgentCommentCommandHandler>.Instance);

    private GetCardCommentsQueryHandler BuildQueryHandler() =>
        new(_cardCommentRepository,
            NullLogger<GetCardCommentsQueryHandler>.Instance);

    // ──────────────────────────────────────────────
    // AddHumanCommentCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingCard_AddsHumanCommentAndReturnsSuccessWithId()
    {
        // Arrange
        var card = CreateValidCard();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardCommentRepository.AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardCommentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddHumanCommentCommand
        {
            CardId = card.Id,
            Content = "A human comment.",
            AuthorId = "user-42"
        };

        var sut = BuildHumanHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task Handle_ExistingCard_HumanComment_CallsAddAsyncAndSaveChanges()
    {
        // Arrange
        var card = CreateValidCard();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardCommentRepository.AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardCommentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddHumanCommentCommand
        {
            CardId = card.Id,
            Content = "Persisted comment.",
            AuthorId = "user-42"
        };

        var sut = BuildHumanHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — both persistence calls must happen exactly once
        await _cardCommentRepository.Received(1)
            .AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>());
        await _cardCommentRepository.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HumanComment_CardNotFound_ReturnsFailure()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddHumanCommentCommand
        {
            CardId = Guid.NewGuid(),
            Content = "Comment on ghost card.",
            AuthorId = "user-1"
        };

        var sut = BuildHumanHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_HumanComment_CardNotFound_DoesNotCallAddAsync()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddHumanCommentCommand
        {
            CardId = Guid.NewGuid(),
            Content = "Comment on ghost card.",
            AuthorId = "user-1"
        };

        var sut = BuildHumanHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no persistence must occur
        await _cardCommentRepository.DidNotReceive()
            .AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>());
        await _cardCommentRepository.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddAgentCommentCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingCard_AddsAgentCommentAndReturnsSuccessWithId()
    {
        // Arrange
        var card = CreateValidCard();
        var workflowTriggerId = Guid.NewGuid();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardCommentRepository.AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardCommentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddAgentCommentCommand
        {
            CardId = card.Id,
            Content = "Agent analysis complete.",
            AgentName = "owl-coder",
            WorkflowTriggerId = workflowTriggerId
        };

        var sut = BuildAgentHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldNotBe(Guid.Empty)
        );
    }

    [Fact]
    public async Task Handle_ExistingCard_AgentComment_CallsAddAsyncAndSaveChanges()
    {
        // Arrange
        var card = CreateValidCard();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _cardCommentRepository.AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _cardCommentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddAgentCommentCommand
        {
            CardId = card.Id,
            Content = "Persisted agent comment.",
            AgentName = "owl-tester"
        };

        var sut = BuildAgentHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — both persistence calls must happen exactly once
        await _cardCommentRepository.Received(1)
            .AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>());
        await _cardCommentRepository.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AgentComment_CardNotFound_ReturnsFailure()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddAgentCommentCommand
        {
            CardId = Guid.NewGuid(),
            Content = "Agent comment on ghost card.",
            AgentName = "owl-coder"
        };

        var sut = BuildAgentHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_AgentComment_CardNotFound_DoesNotCallAddAsync()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddAgentCommentCommand
        {
            CardId = Guid.NewGuid(),
            Content = "Agent comment on ghost card.",
            AgentName = "owl-coder"
        };

        var sut = BuildAgentHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no persistence must occur
        await _cardCommentRepository.DidNotReceive()
            .AddAsync(Arg.Any<CardComment>(), Arg.Any<CancellationToken>());
        await _cardCommentRepository.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetCardCommentsQuery
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCardId_ReturnsComments()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var comments = new List<CardCommentDto>
        {
            new(Guid.NewGuid(), cardId, "First comment",  CommentAuthorType.Human, "user-1", null, null, null, now.AddMinutes(-5)),
            new(Guid.NewGuid(), cardId, "Second comment", CommentAuthorType.Agent, null, "owl-coder", null, null, now)
        };

        _cardCommentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardCommentDto>>(comments));

        var query = new GetCardCommentsQuery { CardId = cardId };
        var sut = BuildQueryHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].Content.ShouldBe("First comment"),
            () => result[1].Content.ShouldBe("Second comment")
        );
    }

    [Fact]
    public async Task Handle_ValidCardId_CallsRepositoryWithCorrectCardId()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _cardCommentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardCommentDto>>(new List<CardCommentDto>()));

        var query = new GetCardCommentsQuery { CardId = cardId };
        var sut = BuildQueryHandler();

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert — repository must be called with the exact cardId from the query
        await _cardCommentRepository.Received(1)
            .GetByCardIdAsync(cardId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardWithNoComments_ReturnsEmptyList()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _cardCommentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardCommentDto>>(new List<CardCommentDto>()));

        var query = new GetCardCommentsQuery { CardId = cardId };
        var sut = BuildQueryHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}
