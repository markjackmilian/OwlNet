using OwlNet.Domain.Entities;
using Shouldly;

namespace OwlNet.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the <see cref="WorkflowTrigger"/> and <see cref="WorkflowTriggerAgent"/>
/// domain entities.
/// Covers the <see cref="WorkflowTrigger.Create"/> factory method (validation, boundary values,
/// initial state), the <see cref="WorkflowTrigger.Update"/> mutation method (field updates,
/// validation, timestamp refresh), the <see cref="WorkflowTrigger.Enable"/> /
/// <see cref="WorkflowTrigger.Disable"/> toggle methods, the
/// <see cref="WorkflowTrigger.SetAgents"/> method (replace, clear, timestamp refresh), and the
/// <see cref="WorkflowTriggerAgent.Create"/> factory method (validation, boundary values).
/// </summary>
public sealed class WorkflowTriggerTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static WorkflowTrigger CreateValidTrigger(
        Guid? projectId = null,
        string name = "Code Review on Develop → Review",
        Guid? fromStatusId = null,
        Guid? toStatusId = null,
        string prompt = "Review the code changes and provide feedback.")
    {
        var from = fromStatusId ?? Guid.NewGuid();
        var to = toStatusId ?? Guid.NewGuid();

        // Ensure from != to when both are generated internally
        if (from == to)
        {
            to = Guid.NewGuid();
        }

        return WorkflowTrigger.Create(
            projectId ?? Guid.NewGuid(),
            name,
            from,
            to,
            prompt);
    }

    private static WorkflowTriggerAgent CreateValidAgent(
        Guid? workflowTriggerId = null,
        string agentName = "code-reviewer",
        int sortOrder = 0)
    {
        return WorkflowTriggerAgent.Create(
            workflowTriggerId ?? Guid.NewGuid(),
            agentName,
            sortOrder);
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_ReturnsWorkflowTriggerWithCorrectProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var name = "Deploy on Staging → Production";
        var fromStatusId = Guid.NewGuid();
        var toStatusId = Guid.NewGuid();
        var prompt = "Deploy the application to production environment.";
        var before = DateTimeOffset.UtcNow;

        // Act
        var trigger = WorkflowTrigger.Create(projectId, name, fromStatusId, toStatusId, prompt);

        // Assert
        var after = DateTimeOffset.UtcNow;

        trigger.ShouldSatisfyAllConditions(
            () => trigger.Id.ShouldNotBe(Guid.Empty),
            () => trigger.ProjectId.ShouldBe(projectId),
            () => trigger.Name.ShouldBe(name),
            () => trigger.FromStatusId.ShouldBe(fromStatusId),
            () => trigger.ToStatusId.ShouldBe(toStatusId),
            () => trigger.Prompt.ShouldBe(prompt),
            () => trigger.IsEnabled.ShouldBeTrue(),
            () => trigger.CreatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => trigger.CreatedAt.ShouldBeLessThanOrEqualTo(after),
            () => trigger.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before),
            () => trigger.UpdatedAt.ShouldBeLessThanOrEqualTo(after),
            () => trigger.CreatedAt.ShouldBe(trigger.UpdatedAt),
            () => trigger.TriggerAgents.ShouldBeEmpty()
        );
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Create — Name Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullName_ThrowsArgumentException()
    {
        // Arrange
        string name = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), name, Guid.NewGuid(), Guid.NewGuid(), "Valid prompt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WhitespaceName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), name, Guid.NewGuid(), Guid.NewGuid(), "Valid prompt"));
    }

    [Fact]
    public void Create_NameExceeds150Chars_ThrowsArgumentException()
    {
        // Arrange
        var name = new string('x', 151);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), name, Guid.NewGuid(), Guid.NewGuid(), "Valid prompt"));

        exception.Message.ShouldContain("150");
    }

    [Fact]
    public void Create_NameExactly150Chars_Succeeds()
    {
        // Arrange
        var name = new string('x', 150);

        // Act
        var trigger = WorkflowTrigger.Create(Guid.NewGuid(), name, Guid.NewGuid(), Guid.NewGuid(), "Valid prompt");

        // Assert
        trigger.Name.ShouldBe(name);
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Create — Prompt Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullPrompt_ThrowsArgumentException()
    {
        // Arrange
        string prompt = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), "Valid name", Guid.NewGuid(), Guid.NewGuid(), prompt));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WhitespacePrompt_ThrowsArgumentException(string prompt)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), "Valid name", Guid.NewGuid(), Guid.NewGuid(), prompt));
    }

    [Fact]
    public void Create_PromptExceeds10000Chars_ThrowsArgumentException()
    {
        // Arrange
        var prompt = new string('p', 10_001);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), "Valid name", Guid.NewGuid(), Guid.NewGuid(), prompt));

        exception.Message.ShouldContain("10 000");
    }

    [Fact]
    public void Create_PromptExactly10000Chars_Succeeds()
    {
        // Arrange
        var prompt = new string('p', 10_000);

        // Act
        var trigger = WorkflowTrigger.Create(Guid.NewGuid(), "Valid name", Guid.NewGuid(), Guid.NewGuid(), prompt);

        // Assert
        trigger.Prompt.ShouldBe(prompt);
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Create — Status Transition Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_SameFromAndToStatus_ThrowsArgumentException()
    {
        // Arrange
        var sameStatusId = Guid.NewGuid();

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => WorkflowTrigger.Create(Guid.NewGuid(), "Valid name", sameStatusId, sameStatusId, "Valid prompt"));

        exception.Message.ShouldContain("Source and destination status must be different");
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Update — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Update_ValidParameters_UpdatesAllFields()
    {
        // Arrange
        var trigger = CreateValidTrigger(name: "Original Name", prompt: "Original prompt");
        var newName = "Updated Trigger Name";
        var newFromStatusId = Guid.NewGuid();
        var newToStatusId = Guid.NewGuid();
        var newPrompt = "Updated prompt for agents.";
        var newIsEnabled = false;

        // Act
        trigger.Update(newName, newFromStatusId, newToStatusId, newPrompt, newIsEnabled);

        // Assert
        trigger.ShouldSatisfyAllConditions(
            () => trigger.Name.ShouldBe(newName),
            () => trigger.FromStatusId.ShouldBe(newFromStatusId),
            () => trigger.ToStatusId.ShouldBe(newToStatusId),
            () => trigger.Prompt.ShouldBe(newPrompt),
            () => trigger.IsEnabled.ShouldBeFalse()
        );
    }

    [Fact]
    public void Update_ValidParameters_RefreshesUpdatedAt()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        var createdAt = trigger.CreatedAt;
        var updatedAtBefore = trigger.UpdatedAt;

        // Act
        trigger.Update("New Name", Guid.NewGuid(), Guid.NewGuid(), "New prompt", true);

        // Assert
        trigger.ShouldSatisfyAllConditions(
            () => trigger.CreatedAt.ShouldBe(createdAt),
            () => trigger.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore)
        );
    }

    [Fact]
    public void Update_SameFromAndToStatus_ThrowsArgumentException()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        var sameStatusId = Guid.NewGuid();

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => trigger.Update("Valid name", sameStatusId, sameStatusId, "Valid prompt", true));

        exception.Message.ShouldContain("Source and destination status must be different");
    }

    [Fact]
    public void Update_IsEnabledFalse_SetsIsEnabledFalse()
    {
        // Arrange
        var trigger = CreateValidTrigger();

        // Act
        trigger.Update("Valid name", Guid.NewGuid(), Guid.NewGuid(), "Valid prompt", isEnabled: false);

        // Assert
        trigger.IsEnabled.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.Enable / Disable
    // ──────────────────────────────────────────────

    [Fact]
    public void Enable_WhenDisabled_SetsIsEnabledTrue()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        trigger.Disable();

        // Act
        trigger.Enable();

        // Assert
        trigger.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Disable_WhenEnabled_SetsIsEnabledFalse()
    {
        // Arrange
        var trigger = CreateValidTrigger();

        // Act
        trigger.Disable();

        // Assert
        trigger.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Enable_RefreshesUpdatedAt()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        trigger.Disable();
        var updatedAtAfterDisable = trigger.UpdatedAt;

        // Act
        trigger.Enable();

        // Assert
        trigger.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtAfterDisable);
    }

    [Fact]
    public void Disable_RefreshesUpdatedAt()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        var updatedAtBefore = trigger.UpdatedAt;

        // Act
        trigger.Disable();

        // Assert
        trigger.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // WorkflowTrigger.SetAgents
    // ──────────────────────────────────────────────

    [Fact]
    public void SetAgents_WithAgents_ReplacesAgentList()
    {
        // Arrange
        var triggerId = Guid.NewGuid();
        var trigger = CreateValidTrigger();

        var agents = new[]
        {
            WorkflowTriggerAgent.Create(triggerId, "agent-alpha", 0),
            WorkflowTriggerAgent.Create(triggerId, "agent-beta", 1),
            WorkflowTriggerAgent.Create(triggerId, "agent-gamma", 2)
        };

        // Act
        trigger.SetAgents(agents);

        // Assert
        trigger.TriggerAgents.ShouldSatisfyAllConditions(
            () => trigger.TriggerAgents.Count.ShouldBe(3),
            () => trigger.TriggerAgents[0].AgentName.ShouldBe("agent-alpha"),
            () => trigger.TriggerAgents[1].AgentName.ShouldBe("agent-beta"),
            () => trigger.TriggerAgents[2].AgentName.ShouldBe("agent-gamma")
        );
    }

    [Fact]
    public void SetAgents_CalledTwice_ReplacesWithLatestList()
    {
        // Arrange
        var triggerId = Guid.NewGuid();
        var trigger = CreateValidTrigger();

        var firstAgents = new[] { WorkflowTriggerAgent.Create(triggerId, "old-agent", 0) };
        var secondAgents = new[] { WorkflowTriggerAgent.Create(triggerId, "new-agent", 0) };

        trigger.SetAgents(firstAgents);

        // Act
        trigger.SetAgents(secondAgents);

        // Assert
        trigger.TriggerAgents.ShouldSatisfyAllConditions(
            () => trigger.TriggerAgents.Count.ShouldBe(1),
            () => trigger.TriggerAgents[0].AgentName.ShouldBe("new-agent")
        );
    }

    [Fact]
    public void SetAgents_EmptyList_ClearsAgentList()
    {
        // Arrange
        var triggerId = Guid.NewGuid();
        var trigger = CreateValidTrigger();
        trigger.SetAgents([WorkflowTriggerAgent.Create(triggerId, "existing-agent", 0)]);

        // Act
        trigger.SetAgents([]);

        // Assert
        trigger.TriggerAgents.ShouldBeEmpty();
    }

    [Fact]
    public void SetAgents_RefreshesUpdatedAt()
    {
        // Arrange
        var trigger = CreateValidTrigger();
        var updatedAtBefore = trigger.UpdatedAt;

        // Act
        trigger.SetAgents([]);

        // Assert
        trigger.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtBefore);
    }

    // ──────────────────────────────────────────────
    // WorkflowTriggerAgent.Create — Happy Path
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_ReturnsAgentWithCorrectProperties()
    {
        // Arrange
        var workflowTriggerId = Guid.NewGuid();
        var agentName = "security-scanner";
        var sortOrder = 3;

        // Act
        var agent = WorkflowTriggerAgent.Create(workflowTriggerId, agentName, sortOrder);

        // Assert
        agent.ShouldSatisfyAllConditions(
            () => agent.Id.ShouldNotBe(Guid.Empty),
            () => agent.WorkflowTriggerId.ShouldBe(workflowTriggerId),
            () => agent.AgentName.ShouldBe(agentName),
            () => agent.SortOrder.ShouldBe(sortOrder)
        );
    }

    // ──────────────────────────────────────────────
    // WorkflowTriggerAgent.Create — AgentName Validation
    // ──────────────────────────────────────────────

    [Fact]
    public void Create_NullAgentName_ThrowsArgumentException()
    {
        // Arrange
        string agentName = null!;

        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTriggerAgent.Create(Guid.NewGuid(), agentName, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WhitespaceAgentName_ThrowsArgumentException(string agentName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(
            () => WorkflowTriggerAgent.Create(Guid.NewGuid(), agentName, 0));
    }

    [Fact]
    public void Create_AgentNameExceeds200Chars_ThrowsArgumentException()
    {
        // Arrange
        var agentName = new string('a', 201);

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => WorkflowTriggerAgent.Create(Guid.NewGuid(), agentName, 0));

        exception.Message.ShouldContain("200");
    }

    [Fact]
    public void Create_AgentNameExactly200Chars_Succeeds()
    {
        // Arrange
        var agentName = new string('a', 200);

        // Act
        var agent = WorkflowTriggerAgent.Create(Guid.NewGuid(), agentName, 0);

        // Assert
        agent.AgentName.ShouldBe(agentName);
    }
}
