using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwlNet.Application.Cards.Commands.AddCardAttachment;
using OwlNet.Application.Cards.Queries.GetCardAttachmentContent;
using OwlNet.Application.Cards.Queries.GetCardAttachments;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Application.Common.Models;
using OwlNet.Domain.Entities;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Application.Cards;

/// <summary>
/// Unit tests for the Card Attachment CQRS handlers:
/// <see cref="AddCardAttachmentCommandHandler"/>, <see cref="GetCardAttachmentsQueryHandler"/>,
/// and <see cref="GetCardAttachmentContentQueryHandler"/>.
/// Each handler is tested for its happy path, repository interaction verification,
/// not-found failure scenarios, and the resulting guard against unnecessary persistence calls.
/// </summary>
public sealed class CardAttachmentHandlerTests
{
    private readonly ICardRepository _cardRepository;
    private readonly IWorkflowTriggerRepository _workflowTriggerRepository;
    private readonly ICardAttachmentRepository _attachmentRepository;

    public CardAttachmentHandlerTests()
    {
        _cardRepository = Substitute.For<ICardRepository>();
        _workflowTriggerRepository = Substitute.For<IWorkflowTriggerRepository>();
        _attachmentRepository = Substitute.For<ICardAttachmentRepository>();
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

    private static WorkflowTrigger CreateValidTrigger(Guid projectId) =>
        WorkflowTrigger.Create(
            projectId: projectId,
            name: "Test Trigger",
            fromStatusId: Guid.NewGuid(),
            toStatusId: Guid.NewGuid(),
            prompt: "Test prompt");

    private AddCardAttachmentCommandHandler BuildAddHandler() =>
        new(_cardRepository, _workflowTriggerRepository, _attachmentRepository,
            NullLogger<AddCardAttachmentCommandHandler>.Instance);

    private GetCardAttachmentsQueryHandler BuildGetListHandler() =>
        new(_attachmentRepository,
            NullLogger<GetCardAttachmentsQueryHandler>.Instance);

    private GetCardAttachmentContentQueryHandler BuildGetContentHandler() =>
        new(_attachmentRepository,
            NullLogger<GetCardAttachmentContentQueryHandler>.Instance);

    // ──────────────────────────────────────────────
    // AddCardAttachmentCommand — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithAttachmentId()
    {
        // Arrange
        var card = CreateValidCard();
        var trigger = CreateValidTrigger(card.ProjectId);

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _workflowTriggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));
        _attachmentRepository.AddAsync(Arg.Any<CardAttachment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _attachmentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddCardAttachmentCommand
        {
            CardId = card.Id,
            FileName = "code-review.md",
            Content = "# Code Review\n\nAll looks good.",
            AgentName = "owl-coder",
            WorkflowTriggerId = trigger.Id
        };

        var sut = BuildAddHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

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
        var card = CreateValidCard();
        var trigger = CreateValidTrigger(card.ProjectId);

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _workflowTriggerRepository.GetEntityByIdAsync(trigger.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(trigger));
        _attachmentRepository.AddAsync(Arg.Any<CardAttachment>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _attachmentRepository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var command = new AddCardAttachmentCommand
        {
            CardId = card.Id,
            FileName = "analysis.md",
            Content = "# Analysis",
            AgentName = "owl-tester",
            WorkflowTriggerId = trigger.Id
        };

        var sut = BuildAddHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — both persistence calls must happen exactly once
        await _attachmentRepository.Received(1)
            .AddAsync(Arg.Any<CardAttachment>(), Arg.Any<CancellationToken>());
        await _attachmentRepository.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddCardAttachmentCommand — Card Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_CardNotFound_ReturnsFailure()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddCardAttachmentCommand
        {
            CardId = Guid.NewGuid(),
            FileName = "report.md",
            Content = "# Report",
            AgentName = "owl-coder",
            WorkflowTriggerId = Guid.NewGuid()
        };

        var sut = BuildAddHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_CardNotFound_DoesNotCallAddAsync()
    {
        // Arrange
        _cardRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(result: null));

        var command = new AddCardAttachmentCommand
        {
            CardId = Guid.NewGuid(),
            FileName = "report.md",
            Content = "# Report",
            AgentName = "owl-coder",
            WorkflowTriggerId = Guid.NewGuid()
        };

        var sut = BuildAddHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no persistence must occur
        await _attachmentRepository.DidNotReceive()
            .AddAsync(Arg.Any<CardAttachment>(), Arg.Any<CancellationToken>());
        await _attachmentRepository.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // AddCardAttachmentCommand — Trigger Not Found
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_TriggerNotFound_ReturnsFailure()
    {
        // Arrange
        var card = CreateValidCard();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _workflowTriggerRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(null));

        var command = new AddCardAttachmentCommand
        {
            CardId = card.Id,
            FileName = "report.md",
            Content = "# Report",
            AgentName = "owl-coder",
            WorkflowTriggerId = Guid.NewGuid()
        };

        var sut = BuildAddHandler();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_TriggerNotFound_DoesNotCallAddAsync()
    {
        // Arrange
        var card = CreateValidCard();

        _cardRepository.GetEntityByIdAsync(card.Id, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Card?>(card));
        _workflowTriggerRepository.GetEntityByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowTrigger?>(null));

        var command = new AddCardAttachmentCommand
        {
            CardId = card.Id,
            FileName = "report.md",
            Content = "# Report",
            AgentName = "owl-coder",
            WorkflowTriggerId = Guid.NewGuid()
        };

        var sut = BuildAddHandler();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — short-circuit: no persistence must occur
        await _attachmentRepository.DidNotReceive()
            .AddAsync(Arg.Any<CardAttachment>(), Arg.Any<CancellationToken>());
        await _attachmentRepository.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────
    // GetCardAttachmentsQuery
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCardId_ReturnsAttachments()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dtos = new List<CardAttachmentDto>
        {
            new(Guid.NewGuid(), cardId, "code-review.md",   "owl-coder",  triggerId, "Code Review Trigger", now.AddMinutes(-10)),
            new(Guid.NewGuid(), cardId, "test-summary.md",  "owl-tester", triggerId, "Code Review Trigger", now)
        };

        _attachmentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardAttachmentDto>>(dtos));

        var query = new GetCardAttachmentsQuery { CardId = cardId };
        var sut = BuildGetListHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result[0].FileName.ShouldBe("code-review.md"),
            () => result[1].FileName.ShouldBe("test-summary.md")
        );
    }

    [Fact]
    public async Task Handle_ValidCardId_CallsRepositoryWithCorrectCardId()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _attachmentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardAttachmentDto>>(new List<CardAttachmentDto>()));

        var query = new GetCardAttachmentsQuery { CardId = cardId };
        var sut = BuildGetListHandler();

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert — repository must be called with the exact cardId from the query
        await _attachmentRepository.Received(1)
            .GetByCardIdAsync(cardId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardWithNoAttachments_ReturnsEmptyList()
    {
        // Arrange
        var cardId = Guid.NewGuid();

        _attachmentRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<List<CardAttachmentDto>>(new List<CardAttachmentDto>()));

        var query = new GetCardAttachmentsQuery { CardId = cardId };
        var sut = BuildGetListHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────
    // GetCardAttachmentContentQuery
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingAttachment_ReturnsSuccessWithContent()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();
        const string expectedContent = "# Report content";

        _attachmentRepository.GetContentByIdAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>(expectedContent));

        var query = new GetCardAttachmentContentQuery { AttachmentId = attachmentId };
        var sut = BuildGetContentHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(expectedContent)
        );
    }

    [Fact]
    public async Task Handle_AttachmentNotFound_ReturnsFailure()
    {
        // Arrange
        _attachmentRepository.GetContentByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>(result: null));

        var query = new GetCardAttachmentContentQuery { AttachmentId = Guid.NewGuid() };
        var sut = BuildGetContentHandler();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.IsFailure.ShouldBeTrue(),
            () => result.Error.ShouldContain("not found", Case.Insensitive)
        );
    }

    [Fact]
    public async Task Handle_ExistingAttachment_CallsRepositoryWithCorrectAttachmentId()
    {
        // Arrange
        var attachmentId = Guid.NewGuid();

        _attachmentRepository.GetContentByIdAsync(attachmentId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("# content"));

        var query = new GetCardAttachmentContentQuery { AttachmentId = attachmentId };
        var sut = BuildGetContentHandler();

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert — repository must be called with the exact attachmentId from the query
        await _attachmentRepository.Received(1)
            .GetContentByIdAsync(attachmentId, Arg.Any<CancellationToken>());
    }
}
