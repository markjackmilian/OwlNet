using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowTrigger"/> entity.
/// Maps the entity to the <c>WorkflowTriggers</c> table and configures:
/// <list type="bullet">
///   <item>Cascade delete from <see cref="Project"/> (trigger is owned by the project).</item>
///   <item>Restrict delete on both <see cref="BoardStatus"/> FKs — triggers retain stale
///   references when a status is deleted; the guard lives in the delete handler.</item>
///   <item>Cascade delete of <see cref="WorkflowTriggerAgent"/> records when the trigger
///   is removed.</item>
///   <item>Indexes for the two primary query patterns: by project and by transition.</item>
/// </list>
/// </summary>
public sealed class WorkflowTriggerConfiguration : IEntityTypeConfiguration<WorkflowTrigger>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
    {
        builder.ToTable("WorkflowTriggers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.FromStatusId)
            .IsRequired();

        builder.Property(t => t.ToStatusId)
            .IsRequired();

        // Prompt: max 10 000 chars enforced at application level (FluentValidation + domain entity).
        // SQL Server does not support nvarchar(N) for N > 4000 in fixed-length columns;
        // nvarchar(max) is used intentionally. The 10 000-char limit is enforced by the application layer.
        builder.Property(t => t.Prompt)
            .IsRequired()
            .HasMaxLength(10_000);

        builder.Property(t => t.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(t => t.ProjectId)
            .IsRequired();

        // Index for GetByProjectId queries.
        builder.HasIndex(t => t.ProjectId);

        // Composite index for GetByTransition queries (trigger evaluation hot path).
        builder.HasIndex(t => new { t.ProjectId, t.FromStatusId, t.ToStatusId });

        // FK → Project: cascade-delete triggers when the owning project is removed.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → BoardStatus (source status): restrict deletion of a status that is
        // still referenced by one or more triggers. The trigger retains the stale reference;
        // the guard that prevents status deletion is implemented in DeleteBoardStatusCommandHandler.
        builder.HasOne<BoardStatus>()
            .WithMany()
            .HasForeignKey(t => t.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK → BoardStatus (destination status): same Restrict policy as FromStatusId.
        builder.HasOne<BoardStatus>()
            .WithMany()
            .HasForeignKey(t => t.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: cascade-delete agent associations when the owning trigger is removed.
        // The backing field _triggerAgents is used by the domain entity to expose TriggerAgents
        // as IReadOnlyList<WorkflowTriggerAgent>; EF Core must be told to use it for change tracking.
        builder.HasMany(t => t.TriggerAgents)
            .WithOne()
            .HasForeignKey(a => a.WorkflowTriggerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.TriggerAgents)
            .HasField("_triggerAgents");
    }
}
