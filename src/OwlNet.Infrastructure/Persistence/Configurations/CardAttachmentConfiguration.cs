using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="CardAttachment"/> entity.
/// Maps the entity to the <c>CardAttachments</c> table and configures:
/// <list type="bullet">
///   <item>
///     <description>
///       Cascade delete from <see cref="Card"/> — deleting a card removes all its attachments.
///     </description>
///   </item>
///   <item>
///     <description>
///       Set-null on <see cref="WorkflowTrigger"/> deletion — the attachment is retained but
///       <see cref="CardAttachment.WorkflowTriggerId"/> is set to <see langword="null"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="CardAttachment.Content"/> mapped to an unbounded text type (<c>TEXT</c> for
///       SQLite, <c>nvarchar(max)</c> for SQL Server) via <c>HasMaxLength(int.MaxValue)</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       Index on <see cref="CardAttachment.CardId"/> for efficient retrieval by card.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class CardAttachmentConfiguration : IEntityTypeConfiguration<CardAttachment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CardAttachment> builder)
    {
        builder.ToTable("CardAttachments");

        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.CardId)
            .IsRequired();

        builder.Property(ca => ca.FileName)
            .IsRequired()
            .HasMaxLength(200);

        // Content: unbounded text — no max length enforced at the DB level.
        // HasMaxLength(int.MaxValue) maps to TEXT on SQLite and nvarchar(max) on SQL Server
        // without requiring provider-specific HasColumnType overrides.
        builder.Property(ca => ca.Content)
            .IsRequired()
            .HasMaxLength(int.MaxValue);

        builder.Property(ca => ca.AgentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ca => ca.WorkflowTriggerId)
            .IsRequired(false);

        builder.Property(ca => ca.CreatedAt)
            .IsRequired();

        // Index on CardId for efficient retrieval of all attachments for a given card.
        builder.HasIndex(ca => ca.CardId);

        // FK → Card: cascade-delete all attachments when the owning card is removed.
        builder.HasOne<Card>()
            .WithMany(c => c.Attachments)
            .HasForeignKey(ca => ca.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → WorkflowTrigger: set WorkflowTriggerId to null when the referenced trigger is deleted.
        // The attachment itself is retained — this preserves the agent output record.
        builder.HasOne<WorkflowTrigger>()
            .WithMany()
            .HasForeignKey(ca => ca.WorkflowTriggerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
