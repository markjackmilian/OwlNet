using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowTriggerAgent"/> entity.
/// Maps the entity to the <c>WorkflowTriggerAgents</c> table.
/// The FK relationship and cascade-delete behaviour are owned by
/// <see cref="WorkflowTriggerConfiguration"/>; this configuration covers only the
/// scalar properties and the index used to retrieve all agents for a given trigger.
/// </summary>
public sealed class WorkflowTriggerAgentConfiguration : IEntityTypeConfiguration<WorkflowTriggerAgent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowTriggerAgent> builder)
    {
        builder.ToTable("WorkflowTriggerAgents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.WorkflowTriggerId)
            .IsRequired();

        builder.Property(a => a.AgentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.SortOrder)
            .IsRequired();

        // Index for queries that retrieve all agents belonging to a specific trigger.
        builder.HasIndex(a => a.WorkflowTriggerId);
    }
}
