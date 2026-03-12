using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="CardComment"/> entity.
/// Maps the entity to the <c>CardComments</c> table and configures:
/// <list type="bullet">
///   <item>
///     <description>
///       Cascade delete from <see cref="Card"/> — deleting a card removes all its comments.
///     </description>
///   </item>
///   <item>
///     <description>
///       Set-null on <see cref="WorkflowTrigger"/> deletion — the comment is retained but
///       <see cref="CardComment.WorkflowTriggerId"/> is set to <see langword="null"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="CardComment.AuthorType"/> stored as <c>int</c> for compact, stable
///       representation of the <c>CommentAuthorType</c> enum.
///     </description>
///   </item>
///   <item>
///     <description>
///       Composite index on (<see cref="CardComment.CardId"/>, <see cref="CardComment.CreatedAt"/>)
///       for efficient chronological retrieval of a card's comment thread.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class CardCommentConfiguration : IEntityTypeConfiguration<CardComment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CardComment> builder)
    {
        builder.ToTable("CardComments");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.CardId)
            .IsRequired();

        // Content: max 10 000 chars enforced at application level (FluentValidation + domain entity).
        // SQL Server does not support nvarchar(N) for N > 4000 in fixed-length columns;
        // nvarchar(max) is used intentionally. The 10 000-char limit is enforced by the application layer.
        builder.Property(cc => cc.Content)
            .IsRequired()
            .HasMaxLength(10_000);

        // Store the enum as an integer for a compact, stable column representation.
        builder.Property(cc => cc.AuthorType)
            .IsRequired()
            .HasConversion<int>();

        // AuthorId: nullable — populated only for Human comments.
        // Max 450 characters matches ASP.NET Core Identity's nvarchar(450) user ID column.
        builder.Property(cc => cc.AuthorId)
            .IsRequired(false)
            .HasMaxLength(450);

        // AgentName: nullable — populated only for Agent comments.
        builder.Property(cc => cc.AgentName)
            .IsRequired(false)
            .HasMaxLength(200);

        // WorkflowTriggerId: nullable — set only when an agent comment originates from a trigger execution.
        builder.Property(cc => cc.WorkflowTriggerId)
            .IsRequired(false);

        builder.Property(cc => cc.CreatedAt)
            .IsRequired();

        // Composite index for chronological comment retrieval: GetCardCommentsQuery orders by CreatedAt ASC.
        builder.HasIndex(cc => new { cc.CardId, cc.CreatedAt });

        // FK → Card: cascade-delete all comments when the owning card is removed.
        // Uses the typed Comments navigation property; EF Core automatically accesses the
        // _comments backing field via its field-access conventions (UsePropertyAccessMode / field name matching).
        builder.HasOne<Card>()
            .WithMany(c => c.Comments)
            .HasForeignKey(cc => cc.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → WorkflowTrigger: set WorkflowTriggerId to null when the referenced trigger is deleted.
        // The comment itself is retained — this preserves the audit trail.
        builder.HasOne<WorkflowTrigger>()
            .WithMany()
            .HasForeignKey(cc => cc.WorkflowTriggerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
