using OwlNet.Application.Common.Models;
using Shouldly;

namespace OwlNet.Tests.Web.Workflow;

// ═══════════════════════════════════════════════════════════════════════════════
//  Logic simulator — replicates the pure state logic of WorkflowTriggersPage
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Replicates the pure, side-effect-free logic of <c>WorkflowTriggersPage.razor.cs</c>
/// in a plain C# class so it can be exercised without a Blazor rendering host.
///
/// The logic is kept intentionally identical to the component — any divergence
/// would make the tests meaningless.
/// </summary>
internal sealed class WorkflowTriggersPageLogic
{
    // ── State (mirrors private fields in the component) ──────────────────────

    /// <summary>All triggers loaded from the server (unfiltered).</summary>
    public List<WorkflowTriggerDto> AllTriggers { get; set; } = [];

    /// <summary>Dictionary mapping BoardStatus ID → Name for fast O(1) lookups.</summary>
    public Dictionary<Guid, string> StatusNames { get; set; } = [];

    /// <summary>
    /// The currently selected filter value.
    /// "all" = show all triggers, "enabled" = only enabled, "disabled" = only disabled.
    /// </summary>
    public string FilterValue { get; private set; } = "all";

    // ── Computed property (mirrors FilteredTriggers in the component) ─────────

    /// <summary>
    /// The filtered view of <see cref="AllTriggers"/> based on <see cref="FilterValue"/>.
    /// Mirrors the <c>FilteredTriggers</c> computed property in the component.
    /// </summary>
    public IEnumerable<WorkflowTriggerDto> FilteredTriggers => FilterValue switch
    {
        "enabled"  => AllTriggers.Where(t => t.IsEnabled),
        "disabled" => AllTriggers.Where(t => !t.IsEnabled),
        _          => AllTriggers
    };

    // ── Methods (mirror private methods in the component) ────────────────────

    /// <summary>
    /// Updates the active filter.
    /// Mirrors <c>OnFilterChanged(string value)</c> in the component.
    /// </summary>
    public void OnFilterChanged(string value)
    {
        FilterValue = value;
    }

    /// <summary>
    /// Resolves a BoardStatus ID to its display name.
    /// Mirrors <c>GetStatusName(Guid statusId)</c> in the component.
    /// </summary>
    public string? GetStatusName(Guid statusId)
    {
        return StatusNames.TryGetValue(statusId, out var name) ? name : null;
    }

    /// <summary>
    /// Returns true if either the FromStatus or ToStatus referenced by the trigger
    /// no longer exists in the project's board statuses.
    /// Mirrors <c>HasStaleStatusReference(WorkflowTriggerDto trigger)</c> in the component.
    /// </summary>
    public bool HasStaleStatusReference(WorkflowTriggerDto trigger)
    {
        return !StatusNames.ContainsKey(trigger.FromStatusId)
            || !StatusNames.ContainsKey(trigger.ToStatusId);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Test class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Unit tests for the pure state logic of <c>WorkflowTriggersPage.razor.cs</c>.
///
/// Because the component methods are private and live inside a Blazor partial class,
/// the tests exercise a <see cref="WorkflowTriggersPageLogic"/> simulator that
/// replicates the identical logic. This lets us verify every filter, lookup, and
/// stale-reference check without a Blazor rendering host.
/// </summary>
public sealed class WorkflowTriggersPageTests
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

    // ──────────────────────────────────────────────────────────────────────────
    //  FilteredTriggers — filter "all"
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FilteredTriggers_WithAllFilter_ReturnsAllTriggers()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers =
            [
                CreateTrigger(isEnabled: true),
                CreateTrigger(isEnabled: false),
                CreateTrigger(isEnabled: true)
            ]
        };

        // Act — default filter is "all", no change needed
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void FilteredTriggers_WithAllFilter_AfterExplicitChange_ReturnsAllTriggers()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers =
            [
                CreateTrigger(isEnabled: true),
                CreateTrigger(isEnabled: false)
            ]
        };
        logic.OnFilterChanged("all");

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.Count.ShouldBe(2);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  FilteredTriggers — filter "enabled"
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FilteredTriggers_WithEnabledFilter_ReturnsOnlyEnabledTriggers()
    {
        // Arrange
        var enabledTrigger1 = CreateTrigger(isEnabled: true, name: "Enabled A");
        var enabledTrigger2 = CreateTrigger(isEnabled: true, name: "Enabled B");
        var disabledTrigger = CreateTrigger(isEnabled: false, name: "Disabled");

        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers = [enabledTrigger1, disabledTrigger, enabledTrigger2]
        };
        logic.OnFilterChanged("enabled");

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(2),
            () => result.ShouldAllBe(t => t.IsEnabled),
            () => result.ShouldContain(t => t.Name == "Enabled A"),
            () => result.ShouldContain(t => t.Name == "Enabled B"),
            () => result.ShouldNotContain(t => t.Name == "Disabled")
        );
    }

    [Fact]
    public void FilteredTriggers_WithEnabledFilter_WhenNoEnabledTriggers_ReturnsEmpty()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers =
            [
                CreateTrigger(isEnabled: false),
                CreateTrigger(isEnabled: false)
            ]
        };
        logic.OnFilterChanged("enabled");

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  FilteredTriggers — filter "disabled"
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FilteredTriggers_WithDisabledFilter_ReturnsOnlyDisabledTriggers()
    {
        // Arrange
        var enabledTrigger  = CreateTrigger(isEnabled: true,  name: "Enabled");
        var disabledTrigger = CreateTrigger(isEnabled: false, name: "Disabled");

        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers = [enabledTrigger, disabledTrigger]
        };
        logic.OnFilterChanged("disabled");

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(1),
            () => result.ShouldAllBe(t => !t.IsEnabled),
            () => result.ShouldContain(t => t.Name == "Disabled"),
            () => result.ShouldNotContain(t => t.Name == "Enabled")
        );
    }

    [Fact]
    public void FilteredTriggers_WithDisabledFilter_WhenNoDisabledTriggers_ReturnsEmpty()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic
        {
            AllTriggers =
            [
                CreateTrigger(isEnabled: true),
                CreateTrigger(isEnabled: true)
            ]
        };
        logic.OnFilterChanged("disabled");

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  FilteredTriggers — empty list
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("all")]
    [InlineData("enabled")]
    [InlineData("disabled")]
    public void FilteredTriggers_WithEmptyList_ReturnsEmptyForAnyFilter(string filter)
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic { AllTriggers = [] };
        logic.OnFilterChanged(filter);

        // Act
        var result = logic.FilteredTriggers.ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  OnFilterChanged
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("all")]
    [InlineData("enabled")]
    [InlineData("disabled")]
    public void OnFilterChanged_UpdatesFilterValue(string newFilter)
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic();

        // Act
        logic.OnFilterChanged(newFilter);

        // Assert
        logic.FilterValue.ShouldBe(newFilter);
    }

    [Fact]
    public void OnFilterChanged_DefaultFilterIsAll()
    {
        // Arrange & Act
        var logic = new WorkflowTriggersPageLogic();

        // Assert
        logic.FilterValue.ShouldBe("all");
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  GetStatusName
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetStatusName_WhenIdExistsInDictionary_ReturnsName()
    {
        // Arrange
        var statusId = Guid.NewGuid();
        const string expectedName = "In Review";

        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string> { [statusId] = expectedName }
        };

        // Act
        var result = logic.GetStatusName(statusId);

        // Assert
        result.ShouldBe(expectedName);
    }

    [Fact]
    public void GetStatusName_WhenIdNotInDictionary_ReturnsNull()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string>
            {
                [Guid.NewGuid()] = "Some Status"
            }
        };

        // Act
        var result = logic.GetStatusName(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetStatusName_WhenDictionaryIsEmpty_ReturnsNull()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic { StatusNames = [] };

        // Act
        var result = logic.GetStatusName(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HasStaleStatusReference
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasStaleStatusReference_WhenBothStatusIdsAreInDictionary_ReturnsFalse()
    {
        // Arrange
        var fromStatusId = Guid.NewGuid();
        var toStatusId   = Guid.NewGuid();

        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string>
            {
                [fromStatusId] = "Todo",
                [toStatusId]   = "In Progress"
            }
        };

        var trigger = CreateTrigger(fromStatusId: fromStatusId, toStatusId: toStatusId);

        // Act
        var result = logic.HasStaleStatusReference(trigger);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasStaleStatusReference_WhenFromStatusIdNotInDictionary_ReturnsTrue()
    {
        // Arrange
        var toStatusId = Guid.NewGuid();

        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string>
            {
                // fromStatusId is intentionally absent
                [toStatusId] = "Done"
            }
        };

        var trigger = CreateTrigger(fromStatusId: Guid.NewGuid(), toStatusId: toStatusId);

        // Act
        var result = logic.HasStaleStatusReference(trigger);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasStaleStatusReference_WhenToStatusIdNotInDictionary_ReturnsTrue()
    {
        // Arrange
        var fromStatusId = Guid.NewGuid();

        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string>
            {
                [fromStatusId] = "Todo"
                // toStatusId is intentionally absent
            }
        };

        var trigger = CreateTrigger(fromStatusId: fromStatusId, toStatusId: Guid.NewGuid());

        // Act
        var result = logic.HasStaleStatusReference(trigger);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasStaleStatusReference_WhenBothStatusIdsNotInDictionary_ReturnsTrue()
    {
        // Arrange
        var logic = new WorkflowTriggersPageLogic { StatusNames = [] };
        var trigger = CreateTrigger();

        // Act
        var result = logic.HasStaleStatusReference(trigger);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasStaleStatusReference_WhenOnlyFromStatusIdIsStale_ReturnsTrue()
    {
        // Arrange — only ToStatus is in the dictionary; FromStatus was deleted
        var toStatusId = Guid.NewGuid();

        var logic = new WorkflowTriggersPageLogic
        {
            StatusNames = new Dictionary<Guid, string> { [toStatusId] = "Done" }
        };

        var trigger = CreateTrigger(fromStatusId: Guid.NewGuid(), toStatusId: toStatusId);

        // Act
        var result = logic.HasStaleStatusReference(trigger);

        // Assert
        result.ShouldBeTrue();
    }
}
