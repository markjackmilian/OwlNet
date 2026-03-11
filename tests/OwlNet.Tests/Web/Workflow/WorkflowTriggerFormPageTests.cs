using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Web.Workflow;

// ═══════════════════════════════════════════════════════════════════════════════
//  Logic simulator — replicates the pure state logic of WorkflowTriggerFormPage
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Replicates the pure, side-effect-free logic of <c>WorkflowTriggerFormPage.razor.cs</c>
/// in a plain C# class so it can be exercised without a Blazor rendering host.
///
/// The logic is kept intentionally identical to the component — any divergence
/// would make the tests meaningless.
/// </summary>
internal sealed class WorkflowTriggerFormPageLogic
{
    // ── Route parameters (mirrors [Parameter] properties) ────────────────────

    /// <summary>
    /// The trigger ID. <see cref="Guid.Empty"/> means create mode;
    /// a non-empty GUID means edit mode.
    /// </summary>
    public Guid TriggerId { get; set; } = Guid.Empty;

    // ── Form field state (mirrors private fields in the component) ────────────

    /// <summary>The trigger name field value.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The selected "From Status" ID.</summary>
    public Guid FromStatusId { get; set; } = Guid.Empty;

    /// <summary>The selected "To Status" ID.</summary>
    public Guid ToStatusId { get; set; } = Guid.Empty;

    /// <summary>The prompt text field value.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Whether the trigger is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>The set of agent names currently checked in the agent checklist.</summary>
    public List<string> SelectedAgentNames { get; set; } = [];

    /// <summary>The ordered list of selected agent names.</summary>
    public List<string> OrderedAgentNames { get; set; } = [];

    /// <summary>The set of agent names referenced by the trigger that no longer exist.</summary>
    public HashSet<string> StaleAgentNames { get; set; } = [];

    // ── Validation state ──────────────────────────────────────────────────────

    /// <summary>Per-field validation error messages.</summary>
    public Dictionary<string, string> ValidationErrors { get; private set; } = [];

    // ── Dirty / suggestion state ──────────────────────────────────────────────

    /// <summary>True when the user has modified any form field since the page loaded.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>The improved prompt text returned by the LLM.</summary>
    public string SuggestedPrompt { get; set; } = string.Empty;

    /// <summary>True when a prompt suggestion is available and the preview area should be shown.</summary>
    public bool ShowPromptSuggestion { get; set; }

    // ── Available data (mirrors _statuses and _availableAgents) ──────────────

    /// <summary>All board statuses for the project.</summary>
    public List<BoardStatusDto> Statuses { get; set; } = [];

    /// <summary>All agent files available in the project.</summary>
    public IReadOnlyList<AgentFileDto> AvailableAgents { get; set; } = [];

    // ── Computed properties (mirror computed properties in the component) ─────

    /// <summary>
    /// True when the page is in create mode (TriggerId is Guid.Empty).
    /// Mirrors <c>IsCreateMode</c> in the component.
    /// </summary>
    public bool IsCreateMode => TriggerId == Guid.Empty;

    /// <summary>
    /// The statuses available for the "To Status" dropdown.
    /// Excludes the currently selected "From Status".
    /// Mirrors <c>ToStatusOptions</c> in the component.
    /// </summary>
    public IEnumerable<BoardStatusDto> ToStatusOptions =>
        Statuses.Where(s => s.Id != FromStatusId);

    // ── Methods (mirror private methods in the component) ────────────────────

    /// <summary>
    /// Validates all form fields and populates <see cref="ValidationErrors"/>.
    /// Mirrors <c>ValidateForm()</c> in the component.
    /// </summary>
    public bool ValidateForm()
    {
        ValidationErrors = [];

        // Name: required, max 150 characters
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationErrors["name"] = "Name is required.";
        }
        else if (Name.Length > 150)
        {
            ValidationErrors["name"] = "Name must be 150 characters or fewer.";
        }

        // From Status: required
        if (FromStatusId == Guid.Empty)
        {
            ValidationErrors["fromStatus"] = "From Status is required.";
        }

        // To Status: required
        if (ToStatusId == Guid.Empty)
        {
            ValidationErrors["toStatus"] = "To Status is required.";
        }

        // From and To must differ
        if (FromStatusId != Guid.Empty && ToStatusId != Guid.Empty
            && FromStatusId == ToStatusId)
        {
            ValidationErrors["toStatus"] = "To Status must be different from From Status.";
        }

        // Prompt: required, max 10000 characters
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            ValidationErrors["prompt"] = "Prompt is required.";
        }
        else if (Prompt.Length > 10000)
        {
            ValidationErrors["prompt"] = "Prompt must be 10,000 characters or fewer.";
        }

        // At least one agent must be selected
        if (SelectedAgentNames.Count == 0)
        {
            ValidationErrors["agents"] = "At least one agent must be selected.";
        }

        return ValidationErrors.Count == 0;
    }

    /// <summary>
    /// Handles the From Status dropdown value change.
    /// Mirrors <c>OnFromStatusChanged(Guid value)</c> in the component.
    /// </summary>
    public void OnFromStatusChanged(Guid value)
    {
        FromStatusId = value;

        if (ToStatusId == FromStatusId)
        {
            ToStatusId = Guid.Empty;
        }

        MarkDirty();
    }

    /// <summary>
    /// Handles a checkbox toggle in the agent checklist.
    /// Mirrors <c>OnAgentCheckChanged(string agentName, bool isChecked)</c> in the component.
    /// </summary>
    public void OnAgentCheckChanged(string agentName, bool isChecked)
    {
        if (isChecked)
        {
            if (!SelectedAgentNames.Contains(agentName))
            {
                SelectedAgentNames.Add(agentName);
                OrderedAgentNames.Add(agentName);
            }
        }
        else
        {
            SelectedAgentNames.Remove(agentName);
            OrderedAgentNames.Remove(agentName);
        }

        MarkDirty();
    }

    /// <summary>
    /// Accepts the LLM-suggested prompt improvement.
    /// Mirrors <c>AcceptSuggestedPrompt()</c> in the component.
    /// </summary>
    public void AcceptSuggestedPrompt()
    {
        Prompt = SuggestedPrompt;
        ShowPromptSuggestion = false;
        SuggestedPrompt = string.Empty;
        MarkDirty();
    }

    /// <summary>
    /// Discards the LLM-suggested prompt improvement.
    /// Mirrors <c>DiscardSuggestedPrompt()</c> in the component.
    /// </summary>
    public void DiscardSuggestedPrompt()
    {
        ShowPromptSuggestion = false;
        SuggestedPrompt = string.Empty;
    }

    /// <summary>
    /// Pre-populates all form fields from an existing <see cref="WorkflowTriggerDto"/>.
    /// Mirrors <c>PopulateFormFromTrigger(WorkflowTriggerDto trigger)</c> in the component.
    /// </summary>
    public void PopulateFormFromTrigger(WorkflowTriggerDto trigger)
    {
        Name         = trigger.Name;
        FromStatusId = trigger.FromStatusId;
        ToStatusId   = trigger.ToStatusId;
        Prompt       = trigger.Prompt;
        IsEnabled    = trigger.IsEnabled;

        var orderedAgents = trigger.TriggerAgents
            .OrderBy(a => a.SortOrder)
            .Select(a => a.AgentName)
            .ToList();

        OrderedAgentNames  = orderedAgents;
        SelectedAgentNames = new List<string>(orderedAgents);

        var availableNames = AvailableAgents.Select(a => a.FileName).ToHashSet();
        StaleAgentNames = SelectedAgentNames
            .Where(name => !availableNames.Contains(name))
            .ToHashSet();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void MarkDirty() => IsDirty = true;
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Test class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unit tests for the pure state logic of <c>WorkflowTriggerFormPage.razor.cs</c>.
///
/// Because the component methods are private and live inside a Blazor partial class,
/// the tests exercise a <see cref="WorkflowTriggerFormPageLogic"/> simulator that
/// replicates the identical logic. This lets us verify every validation rule,
/// computed property, and state transition without a Blazor rendering host.
/// </summary>
public sealed class WorkflowTriggerFormPageTests
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static WorkflowTriggerDto CreateTrigger(
        Guid? id = null,
        bool isEnabled = true,
        Guid? fromStatusId = null,
        Guid? toStatusId = null,
        string name = "Test Trigger",
        string prompt = "Test prompt",
        IReadOnlyList<WorkflowTriggerAgentDto>? agents = null)
    {
        return new WorkflowTriggerDto(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            fromStatusId ?? Guid.NewGuid(),
            toStatusId ?? Guid.NewGuid(),
            prompt,
            isEnabled,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            agents ?? []);
    }

    private static BoardStatusDto CreateStatus(Guid? id = null, string name = "In Progress")
    {
        return new BoardStatusDto(id ?? Guid.NewGuid(), name, 0, false, Guid.NewGuid());
    }

    private static AgentFileDto CreateAgentFile(string fileName = "my-agent")
    {
        return new AgentFileDto(fileName, $"/agents/{fileName}.md", "auto", "A test agent", "---\nmode: auto\n---");
    }

    /// <summary>
    /// Creates a logic instance pre-populated with all valid form values so that
    /// individual tests can override only the field they want to invalidate.
    /// </summary>
    private static WorkflowTriggerFormPageLogic CreateValidLogic()
    {
        var fromId = Guid.NewGuid();
        var toId   = Guid.NewGuid();

        return new WorkflowTriggerFormPageLogic
        {
            Name               = "My Trigger",
            FromStatusId       = fromId,
            ToStatusId         = toId,
            Prompt             = "Do something useful.",
            SelectedAgentNames = ["agent-a"]
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  IsCreateMode
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsCreateMode_WhenTriggerIdIsEmpty_ReturnsTrue()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic { TriggerId = Guid.Empty };

        // Act & Assert
        logic.IsCreateMode.ShouldBeTrue();
    }

    [Fact]
    public void IsCreateMode_WhenTriggerIdIsNonEmpty_ReturnsFalse()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic { TriggerId = Guid.NewGuid() };

        // Act & Assert
        logic.IsCreateMode.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ToStatusOptions
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToStatusOptions_WhenFromStatusIdIsEmpty_ReturnsAllStatuses()
    {
        // Arrange
        var status1 = CreateStatus(name: "Todo");
        var status2 = CreateStatus(name: "In Progress");
        var status3 = CreateStatus(name: "Done");

        var logic = new WorkflowTriggerFormPageLogic
        {
            Statuses     = [status1, status2, status3],
            FromStatusId = Guid.Empty
        };

        // Act
        var result = logic.ToStatusOptions.ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ToStatusOptions_ExcludesSelectedFromStatus()
    {
        // Arrange
        var fromStatus = CreateStatus(name: "Todo");
        var otherStatus1 = CreateStatus(name: "In Progress");
        var otherStatus2 = CreateStatus(name: "Done");

        var logic = new WorkflowTriggerFormPageLogic
        {
            Statuses     = [fromStatus, otherStatus1, otherStatus2],
            FromStatusId = fromStatus.Id
        };

        // Act
        var result = logic.ToStatusOptions.ToList();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result.ShouldNotContain(s => s.Id == fromStatus.Id),
            () => result.ShouldContain(s => s.Id == otherStatus1.Id),
            () => result.ShouldContain(s => s.Id == otherStatus2.Id)
        );
    }

    [Fact]
    public void ToStatusOptions_WhenOnlyOneStatus_AndItIsFromStatus_ReturnsEmpty()
    {
        // Arrange
        var fromStatus = CreateStatus(name: "Todo");

        var logic = new WorkflowTriggerFormPageLogic
        {
            Statuses     = [fromStatus],
            FromStatusId = fromStatus.Id
        };

        // Act
        var result = logic.ToStatusOptions.ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — happy path
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateForm_WhenAllFieldsAreValid_ReturnsTrue()
    {
        // Arrange
        var logic = CreateValidLogic();

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeTrue(),
            () => logic.ValidationErrors.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — name validation
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateForm_WhenNameIsEmptyOrWhitespace_ReturnsFalseWithNameError(string emptyName)
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.Name = emptyName;

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("name").ShouldBeTrue(),
            () => logic.ValidationErrors["name"].ShouldBe("Name is required.")
        );
    }

    [Fact]
    public void ValidateForm_WhenNameExceeds150Characters_ReturnsFalseWithNameError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.Name = new string('x', 151);

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("name").ShouldBeTrue(),
            () => logic.ValidationErrors["name"].ShouldBe("Name must be 150 characters or fewer.")
        );
    }

    [Fact]
    public void ValidateForm_WhenNameIsExactly150Characters_ReturnsTrue()
    {
        // Arrange — boundary: exactly 150 chars is valid
        var logic = CreateValidLogic();
        logic.Name = new string('x', 150);

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldBeTrue();
        logic.ValidationErrors.ContainsKey("name").ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — fromStatus validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateForm_WhenFromStatusIdIsEmpty_ReturnsFalseWithFromStatusError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.FromStatusId = Guid.Empty;

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("fromStatus").ShouldBeTrue(),
            () => logic.ValidationErrors["fromStatus"].ShouldBe("From Status is required.")
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — toStatus validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateForm_WhenToStatusIdIsEmpty_ReturnsFalseWithToStatusError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.ToStatusId = Guid.Empty;

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("toStatus").ShouldBeTrue(),
            () => logic.ValidationErrors["toStatus"].ShouldBe("To Status is required.")
        );
    }

    [Fact]
    public void ValidateForm_WhenFromAndToStatusAreTheSame_ReturnsFalseWithToStatusError()
    {
        // Arrange — both non-empty but equal
        var sameId = Guid.NewGuid();
        var logic  = CreateValidLogic();
        logic.FromStatusId = sameId;
        logic.ToStatusId   = sameId;

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("toStatus").ShouldBeTrue(),
            () => logic.ValidationErrors["toStatus"].ShouldBe("To Status must be different from From Status.")
        );
    }

    [Fact]
    public void ValidateForm_WhenFromAndToStatusAreDifferentNonEmpty_NoToStatusError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.FromStatusId = Guid.NewGuid();
        logic.ToStatusId   = Guid.NewGuid();

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldBeTrue();
        logic.ValidationErrors.ContainsKey("toStatus").ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — prompt validation
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateForm_WhenPromptIsEmptyOrWhitespace_ReturnsFalseWithPromptError(string emptyPrompt)
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.Prompt = emptyPrompt;

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("prompt").ShouldBeTrue(),
            () => logic.ValidationErrors["prompt"].ShouldBe("Prompt is required.")
        );
    }

    [Fact]
    public void ValidateForm_WhenPromptExceeds10000Characters_ReturnsFalseWithPromptError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.Prompt = new string('x', 10001);

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("prompt").ShouldBeTrue(),
            () => logic.ValidationErrors["prompt"].ShouldBe("Prompt must be 10,000 characters or fewer.")
        );
    }

    [Fact]
    public void ValidateForm_WhenPromptIsExactly10000Characters_ReturnsTrue()
    {
        // Arrange — boundary: exactly 10000 chars is valid
        var logic = CreateValidLogic();
        logic.Prompt = new string('x', 10000);

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldBeTrue();
        logic.ValidationErrors.ContainsKey("prompt").ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — agents validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateForm_WhenNoAgentsSelected_ReturnsFalseWithAgentsError()
    {
        // Arrange
        var logic = CreateValidLogic();
        logic.SelectedAgentNames = [];

        // Act
        var result = logic.ValidateForm();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldBeFalse(),
            () => logic.ValidationErrors.ContainsKey("agents").ShouldBeTrue(),
            () => logic.ValidationErrors["agents"].ShouldBe("At least one agent must be selected.")
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ValidateForm — clears previous errors on each call
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateForm_ClearsPreviousErrorsBeforeValidating()
    {
        // Arrange — first call with invalid data to populate errors
        var logic = CreateValidLogic();
        logic.Name = "";
        logic.ValidateForm();
        logic.ValidationErrors.ContainsKey("name").ShouldBeTrue(); // pre-condition

        // Fix the name and re-validate
        logic.Name = "Fixed Name";

        // Act
        var result = logic.ValidateForm();

        // Assert — name error must be gone
        result.ShouldBeTrue();
        logic.ValidationErrors.ContainsKey("name").ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  OnFromStatusChanged
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnFromStatusChanged_UpdatesFromStatusId()
    {
        // Arrange
        var logic    = new WorkflowTriggerFormPageLogic();
        var newFromId = Guid.NewGuid();

        // Act
        logic.OnFromStatusChanged(newFromId);

        // Assert
        logic.FromStatusId.ShouldBe(newFromId);
    }

    [Fact]
    public void OnFromStatusChanged_SetsDirtyFlag()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic();

        // Act
        logic.OnFromStatusChanged(Guid.NewGuid());

        // Assert
        logic.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void OnFromStatusChanged_WhenToStatusEqualsNewFromStatus_ClearsToStatus()
    {
        // Arrange — ToStatus is pre-set to the same value we are about to set as FromStatus
        var sharedId = Guid.NewGuid();
        var logic    = new WorkflowTriggerFormPageLogic { ToStatusId = sharedId };

        // Act
        logic.OnFromStatusChanged(sharedId);

        // Assert
        logic.ToStatusId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void OnFromStatusChanged_WhenToStatusDiffersFromNewFromStatus_DoesNotClearToStatus()
    {
        // Arrange
        var fromId = Guid.NewGuid();
        var toId   = Guid.NewGuid(); // different from fromId
        var logic  = new WorkflowTriggerFormPageLogic { ToStatusId = toId };

        // Act
        logic.OnFromStatusChanged(fromId);

        // Assert — ToStatus must remain unchanged
        logic.ToStatusId.ShouldBe(toId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  OnAgentCheckChanged
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnAgentCheckChanged_WhenChecked_AddsAgentToSelectedAndOrderedLists()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic();

        // Act
        logic.OnAgentCheckChanged("agent-a", isChecked: true);

        // Assert
        logic.ShouldSatisfyAllConditions(
            () => logic.SelectedAgentNames.ShouldContain("agent-a"),
            () => logic.OrderedAgentNames.ShouldContain("agent-a")
        );
    }

    [Fact]
    public void OnAgentCheckChanged_WhenUnchecked_RemovesAgentFromSelectedAndOrderedLists()
    {
        // Arrange — start with the agent already selected
        var logic = new WorkflowTriggerFormPageLogic
        {
            SelectedAgentNames = ["agent-a"],
            OrderedAgentNames  = ["agent-a"]
        };

        // Act
        logic.OnAgentCheckChanged("agent-a", isChecked: false);

        // Assert
        logic.ShouldSatisfyAllConditions(
            () => logic.SelectedAgentNames.ShouldNotContain("agent-a"),
            () => logic.OrderedAgentNames.ShouldNotContain("agent-a")
        );
    }

    [Fact]
    public void OnAgentCheckChanged_WhenCheckedAndAlreadyPresent_DoesNotAddDuplicate()
    {
        // Arrange — agent is already in both lists
        var logic = new WorkflowTriggerFormPageLogic
        {
            SelectedAgentNames = ["agent-a"],
            OrderedAgentNames  = ["agent-a"]
        };

        // Act
        logic.OnAgentCheckChanged("agent-a", isChecked: true);

        // Assert — still exactly one entry
        logic.ShouldSatisfyAllConditions(
            () => logic.SelectedAgentNames.Count(n => n == "agent-a").ShouldBe(1),
            () => logic.OrderedAgentNames.Count(n => n == "agent-a").ShouldBe(1)
        );
    }

    [Fact]
    public void OnAgentCheckChanged_SetsDirtyFlag()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic();

        // Act
        logic.OnAgentCheckChanged("agent-a", isChecked: true);

        // Assert
        logic.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void OnAgentCheckChanged_WhenChecked_PreservesExistingAgentsInOrder()
    {
        // Arrange — two agents already selected
        var logic = new WorkflowTriggerFormPageLogic
        {
            SelectedAgentNames = ["agent-a", "agent-b"],
            OrderedAgentNames  = ["agent-a", "agent-b"]
        };

        // Act — add a third agent
        logic.OnAgentCheckChanged("agent-c", isChecked: true);

        // Assert — new agent appended at the end; existing order preserved
        logic.ShouldSatisfyAllConditions(
            () => logic.OrderedAgentNames.Count.ShouldBe(3),
            () => logic.OrderedAgentNames[0].ShouldBe("agent-a"),
            () => logic.OrderedAgentNames[1].ShouldBe("agent-b"),
            () => logic.OrderedAgentNames[2].ShouldBe("agent-c")
        );
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  AcceptSuggestedPrompt
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AcceptSuggestedPrompt_ReplacePromptWithSuggestedPrompt()
    {
        // Arrange
        const string originalPrompt  = "Original prompt text.";
        const string suggestedPrompt = "Improved prompt text.";

        var logic = new WorkflowTriggerFormPageLogic
        {
            Prompt               = originalPrompt,
            SuggestedPrompt      = suggestedPrompt,
            ShowPromptSuggestion = true
        };

        // Act
        logic.AcceptSuggestedPrompt();

        // Assert
        logic.Prompt.ShouldBe(suggestedPrompt);
    }

    [Fact]
    public void AcceptSuggestedPrompt_HidesPromptSuggestionArea()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.AcceptSuggestedPrompt();

        // Assert
        logic.ShowPromptSuggestion.ShouldBeFalse();
    }

    [Fact]
    public void AcceptSuggestedPrompt_ClearsSuggestedPrompt()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.AcceptSuggestedPrompt();

        // Assert
        logic.SuggestedPrompt.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public void AcceptSuggestedPrompt_SetsDirtyFlag()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.AcceptSuggestedPrompt();

        // Assert
        logic.IsDirty.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  DiscardSuggestedPrompt
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DiscardSuggestedPrompt_HidesPromptSuggestionArea()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.DiscardSuggestedPrompt();

        // Assert
        logic.ShowPromptSuggestion.ShouldBeFalse();
    }

    [Fact]
    public void DiscardSuggestedPrompt_ClearsSuggestedPrompt()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.DiscardSuggestedPrompt();

        // Assert
        logic.SuggestedPrompt.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public void DiscardSuggestedPrompt_DoesNotModifyOriginalPrompt()
    {
        // Arrange
        const string originalPrompt = "Original prompt text.";

        var logic = new WorkflowTriggerFormPageLogic
        {
            Prompt               = originalPrompt,
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.DiscardSuggestedPrompt();

        // Assert — original prompt must be untouched
        logic.Prompt.ShouldBe(originalPrompt);
    }

    [Fact]
    public void DiscardSuggestedPrompt_DoesNotSetDirtyFlag()
    {
        // Arrange
        var logic = new WorkflowTriggerFormPageLogic
        {
            SuggestedPrompt      = "Better prompt.",
            ShowPromptSuggestion = true
        };

        // Act
        logic.DiscardSuggestedPrompt();

        // Assert — discard does not mark the form as dirty
        logic.IsDirty.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  PopulateFormFromTrigger
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PopulateFormFromTrigger_PopulatesAllScalarFields()
    {
        // Arrange
        var fromId    = Guid.NewGuid();
        var toId      = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var trigger = CreateTrigger(
            id:           triggerId,
            name:         "Code Review",
            fromStatusId: fromId,
            toStatusId:   toId,
            prompt:       "Review the code carefully.",
            isEnabled:    false);

        var logic = new WorkflowTriggerFormPageLogic();

        // Act
        logic.PopulateFormFromTrigger(trigger);

        // Assert
        logic.ShouldSatisfyAllConditions(
            () => logic.Name.ShouldBe("Code Review"),
            () => logic.FromStatusId.ShouldBe(fromId),
            () => logic.ToStatusId.ShouldBe(toId),
            () => logic.Prompt.ShouldBe("Review the code carefully."),
            () => logic.IsEnabled.ShouldBeFalse()
        );
    }

    [Fact]
    public void PopulateFormFromTrigger_PopulatesAgentsOrderedBySortOrder()
    {
        // Arrange — agents intentionally out of order to verify sorting
        var triggerId = Guid.NewGuid();
        var agents = new List<WorkflowTriggerAgentDto>
        {
            new(Guid.NewGuid(), triggerId, "agent-c", 2),
            new(Guid.NewGuid(), triggerId, "agent-a", 0),
            new(Guid.NewGuid(), triggerId, "agent-b", 1)
        };

        var trigger = CreateTrigger(id: triggerId, agents: agents);
        var logic   = new WorkflowTriggerFormPageLogic();

        // Act
        logic.PopulateFormFromTrigger(trigger);

        // Assert — must be sorted by SortOrder ascending
        logic.ShouldSatisfyAllConditions(
            () => logic.OrderedAgentNames.Count.ShouldBe(3),
            () => logic.OrderedAgentNames[0].ShouldBe("agent-a"),
            () => logic.OrderedAgentNames[1].ShouldBe("agent-b"),
            () => logic.OrderedAgentNames[2].ShouldBe("agent-c"),
            () => logic.SelectedAgentNames.ShouldContain("agent-a"),
            () => logic.SelectedAgentNames.ShouldContain("agent-b"),
            () => logic.SelectedAgentNames.ShouldContain("agent-c")
        );
    }

    [Fact]
    public void PopulateFormFromTrigger_CalculatesStaleAgentNames()
    {
        // Arrange — trigger references "agent-deleted" which is not in available agents
        var triggerId = Guid.NewGuid();
        var agents = new List<WorkflowTriggerAgentDto>
        {
            new(Guid.NewGuid(), triggerId, "agent-existing", 0),
            new(Guid.NewGuid(), triggerId, "agent-deleted",  1)
        };

        var trigger = CreateTrigger(id: triggerId, agents: agents);

        var logic = new WorkflowTriggerFormPageLogic
        {
            AvailableAgents = [CreateAgentFile("agent-existing")]
            // "agent-deleted" is intentionally absent from available agents
        };

        // Act
        logic.PopulateFormFromTrigger(trigger);

        // Assert
        logic.ShouldSatisfyAllConditions(
            () => logic.StaleAgentNames.ShouldContain("agent-deleted"),
            () => logic.StaleAgentNames.ShouldNotContain("agent-existing"),
            () => logic.StaleAgentNames.Count.ShouldBe(1)
        );
    }

    [Fact]
    public void PopulateFormFromTrigger_WhenAllAgentsAreAvailable_StaleAgentNamesIsEmpty()
    {
        // Arrange
        var triggerId = Guid.NewGuid();
        var agents = new List<WorkflowTriggerAgentDto>
        {
            new(Guid.NewGuid(), triggerId, "agent-a", 0),
            new(Guid.NewGuid(), triggerId, "agent-b", 1)
        };

        var trigger = CreateTrigger(id: triggerId, agents: agents);

        var logic = new WorkflowTriggerFormPageLogic
        {
            AvailableAgents =
            [
                CreateAgentFile("agent-a"),
                CreateAgentFile("agent-b")
            ]
        };

        // Act
        logic.PopulateFormFromTrigger(trigger);

        // Assert
        logic.StaleAgentNames.ShouldBeEmpty();
    }

    [Fact]
    public void PopulateFormFromTrigger_WhenTriggerHasNoAgents_SelectedAndOrderedListsAreEmpty()
    {
        // Arrange
        var trigger = CreateTrigger(agents: []);
        var logic   = new WorkflowTriggerFormPageLogic();

        // Act
        logic.PopulateFormFromTrigger(trigger);

        // Assert
        logic.ShouldSatisfyAllConditions(
            () => logic.SelectedAgentNames.ShouldBeEmpty(),
            () => logic.OrderedAgentNames.ShouldBeEmpty(),
            () => logic.StaleAgentNames.ShouldBeEmpty()
        );
    }
}
