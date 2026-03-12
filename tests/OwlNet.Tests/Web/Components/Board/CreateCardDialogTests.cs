using OwlNet.Application.Common.Models;
using OwlNet.Domain.Enums;
using Shouldly;

namespace OwlNet.Tests.Web.Components.Board;

// ═══════════════════════════════════════════════════════════════════════════════
//  Logic simulator — replicates the pure state logic of CreateCardDialog
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Replicates the pure, side-effect-free logic of <c>CreateCardDialog.razor.cs</c>
/// in a plain C# class so it can be exercised without a Blazor rendering host.
///
/// The logic is kept intentionally identical to the component — any divergence
/// would make the tests meaningless.
/// </summary>
internal sealed class CreateCardDialogLogic
{
    // ── State (mirrors component fields) ─────────────────────────────────────

    /// <summary>The card title entered by the user.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The board status selected for the new card.</summary>
    public Guid? SelectedStatusId { get; set; }

    /// <summary>The priority selected for the new card. Defaults to Medium.</summary>
    public CardPriority Priority { get; set; } = CardPriority.Medium;

    /// <summary>The optional description entered by the user.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The IDs of tags currently selected for the new card.</summary>
    public List<Guid> SelectedTagIds { get; set; } = [];

    /// <summary>The board statuses available for selection.</summary>
    public List<BoardStatusDto> Statuses { get; set; } = [];

    /// <summary>All project tags available for selection.</summary>
    public IReadOnlyList<ProjectTagDto> AllTags { get; set; } = [];

    // ── Validation (mirrors _titleValidation and _descriptionValidation) ─────

    /// <summary>
    /// Validates the card title.
    /// Mirrors the <c>_titleValidation</c> delegate in the component.
    /// </summary>
    public IEnumerable<string> ValidateTitle(string value)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
            errors.Add("Title is required.");
        else if (value.Trim().Length > 200)
            errors.Add("Title must be 200 characters or fewer.");
        return errors;
    }

    /// <summary>
    /// Validates the card description.
    /// Mirrors the <c>_descriptionValidation</c> delegate in the component.
    /// </summary>
    public IEnumerable<string> ValidateDescription(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > 5000)
            return ["Description must be 5000 characters or fewer."];
        return [];
    }

    // ── Tag toggle (mirrors ToggleTag) ────────────────────────────────────────

    /// <summary>
    /// Adds the tag if not already selected; removes it if it is.
    /// Mirrors <c>ToggleTag(Guid tagId)</c> in the component.
    /// </summary>
    public void ToggleTag(Guid tagId)
    {
        if (SelectedTagIds.Contains(tagId))
            SelectedTagIds.Remove(tagId);
        else
            SelectedTagIds.Add(tagId);
    }

    // ── Submit guard (mirrors IsSubmitDisabled computed property) ─────────────

    /// <summary>
    /// Returns <see langword="true"/> when the submit button should be disabled.
    /// Mirrors the <c>IsSubmitDisabled</c> computed property in the component.
    /// </summary>
    public bool IsSubmitDisabled(bool isLoading, bool isSubmitting) =>
        isSubmitting || isLoading || Statuses.Count == 0;

    // ── Prefilled status (mirrors OnInitializedAsync logic) ───────────────────

    /// <summary>
    /// Applies a pre-selected status ID when the dialog is opened from a specific
    /// board column.
    /// Mirrors the prefill logic in <c>OnInitializedAsync</c>.
    /// </summary>
    public void ApplyPrefilledStatus(Guid? prefilledStatusId)
    {
        if (prefilledStatusId.HasValue)
            SelectedStatusId = prefilledStatusId.Value;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Test class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unit tests for the pure state logic of <c>CreateCardDialog.razor.cs</c>.
///
/// Because the component methods are private and live inside a Blazor partial class,
/// the tests exercise a <see cref="CreateCardDialogLogic"/> simulator that replicates
/// the identical logic. This lets us verify every validation rule, tag toggle, and
/// submit-guard condition without a Blazor rendering host.
///
/// Spec reference: <c>SPEC-C5-card-create.md</c>
/// </summary>
public sealed class CreateCardDialogTests
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static BoardStatusDto CreateStatus(Guid? id = null, string name = "To Do") =>
        new(id ?? Guid.NewGuid(), name, 0, false, Guid.NewGuid());

    private static ProjectTagDto CreateTag(Guid? id = null, string name = "bug") =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), name, "#ff0000", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateTitle — required
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTitle_EmptyString_ReturnsRequiredError()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();

        // Act
        var errors = logic.ValidateTitle("").ToList();

        // Assert
        errors.ShouldContain("Title is required.");
    }

    [Fact]
    public void ValidateTitle_WhitespaceOnly_ReturnsRequiredError()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();

        // Act
        var errors = logic.ValidateTitle("   ").ToList();

        // Assert
        errors.ShouldContain("Title is required.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateTitle — length boundary
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTitle_ExactlyMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();
        var titleAt200Chars = new string('x', 200);

        // Act
        var errors = logic.ValidateTitle(titleAt200Chars).ToList();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateTitle_ExceedsMaxLength_ReturnsLengthError()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();
        var titleAt201Chars = new string('x', 201);

        // Act
        var errors = logic.ValidateTitle(titleAt201Chars).ToList();

        // Assert
        errors.ShouldContain(e => e.Contains("200 characters or fewer"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateTitle — valid input
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateTitle_ValidTitle_ReturnsNoErrors()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();

        // Act
        var errors = logic.ValidateTitle("My Card Title").ToList();

        // Assert
        errors.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateDescription — empty / boundary
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateDescription_EmptyString_ReturnsNoErrors()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();

        // Act
        var errors = logic.ValidateDescription("").ToList();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateDescription_ExactlyMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();
        var descriptionAt5000Chars = new string('a', 5000);

        // Act
        var errors = logic.ValidateDescription(descriptionAt5000Chars).ToList();

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateDescription_ExceedsMaxLength_ReturnsLengthError()
    {
        // Arrange
        var logic = new CreateCardDialogLogic();
        var descriptionAt5001Chars = new string('a', 5001);

        // Act
        var errors = logic.ValidateDescription(descriptionAt5001Chars).ToList();

        // Assert
        errors.ShouldContain(e => e.Contains("5000 characters or fewer"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ToggleTag — add
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToggleTag_TagNotSelected_AddsTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var logic = new CreateCardDialogLogic();

        // Act
        logic.ToggleTag(tagId);

        // Assert
        logic.SelectedTagIds.ShouldSatisfyAllConditions(
            () => logic.SelectedTagIds.ShouldContain(tagId),
            () => logic.SelectedTagIds.Count.ShouldBe(1)
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ToggleTag — remove
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToggleTag_TagAlreadySelected_RemovesTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var logic = new CreateCardDialogLogic
        {
            SelectedTagIds = [tagId]
        };

        // Act
        logic.ToggleTag(tagId);

        // Assert
        logic.SelectedTagIds.ShouldSatisfyAllConditions(
            () => logic.SelectedTagIds.ShouldNotContain(tagId),
            () => logic.SelectedTagIds.Count.ShouldBe(0)
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ToggleTag — multiple toggles
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToggleTag_MultipleToggles_CorrectlyTracksSelection()
    {
        // Arrange
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();
        var tagId3 = Guid.NewGuid();
        var logic = new CreateCardDialogLogic();

        // Act — toggle all three on, then toggle the middle one off
        logic.ToggleTag(tagId1);
        logic.ToggleTag(tagId2);
        logic.ToggleTag(tagId3);
        logic.ToggleTag(tagId2); // remove the middle tag

        // Assert — tagId1 and tagId3 remain; tagId2 is gone
        logic.SelectedTagIds.ShouldSatisfyAllConditions(
            () => logic.SelectedTagIds.Count.ShouldBe(2),
            () => logic.SelectedTagIds.ShouldContain(tagId1),
            () => logic.SelectedTagIds.ShouldNotContain(tagId2),
            () => logic.SelectedTagIds.ShouldContain(tagId3)
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  IsSubmitDisabled
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSubmitDisabled_WhenLoading_ReturnsTrue()
    {
        // Arrange
        var logic = new CreateCardDialogLogic
        {
            Statuses = [CreateStatus()]
        };

        // Act
        var result = logic.IsSubmitDisabled(isLoading: true, isSubmitting: false);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSubmitDisabled_WhenSubmitting_ReturnsTrue()
    {
        // Arrange
        var logic = new CreateCardDialogLogic
        {
            Statuses = [CreateStatus()]
        };

        // Act
        var result = logic.IsSubmitDisabled(isLoading: false, isSubmitting: true);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSubmitDisabled_WhenNoStatuses_ReturnsTrue()
    {
        // Arrange
        var logic = new CreateCardDialogLogic
        {
            Statuses = [] // no statuses loaded yet
        };

        // Act
        var result = logic.IsSubmitDisabled(isLoading: false, isSubmitting: false);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSubmitDisabled_WhenReadyToSubmit_ReturnsFalse()
    {
        // Arrange
        var logic = new CreateCardDialogLogic
        {
            Statuses = [CreateStatus()]
        };

        // Act
        var result = logic.IsSubmitDisabled(isLoading: false, isSubmitting: false);

        // Assert
        result.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ApplyPrefilledStatus
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPrefilledStatus_WithStatusId_SetsSelectedStatusId()
    {
        // Arrange
        var prefilledStatusId = Guid.NewGuid();
        var logic = new CreateCardDialogLogic();

        // Act
        logic.ApplyPrefilledStatus(prefilledStatusId);

        // Assert
        logic.SelectedStatusId.ShouldBe(prefilledStatusId);
    }

    [Fact]
    public void ApplyPrefilledStatus_WithNull_LeavesSelectedStatusIdNull()
    {
        // Arrange
        var logic = new CreateCardDialogLogic(); // SelectedStatusId starts as null

        // Act
        logic.ApplyPrefilledStatus(null);

        // Assert
        logic.SelectedStatusId.ShouldBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Default state
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Priority_DefaultValue_IsMedium()
    {
        // Arrange & Act
        var logic = new CreateCardDialogLogic();

        // Assert
        logic.Priority.ShouldBe(CardPriority.Medium);
    }

    [Fact]
    public void SelectedTagIds_DefaultValue_IsEmpty()
    {
        // Arrange & Act
        var logic = new CreateCardDialogLogic();

        // Assert
        logic.SelectedTagIds.ShouldBeEmpty();
    }
}
