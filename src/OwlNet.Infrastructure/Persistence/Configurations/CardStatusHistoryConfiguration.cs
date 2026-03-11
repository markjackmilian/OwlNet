using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="CardStatusHistory"/> entity.
/// Maps the entity to the <c>CardStatusHistories</c> table and configures foreign keys
/// to <see cref="Card"/> and <see cref="BoardStatus"/> with appropriate delete behaviours.
/// </summary>
public sealed class CardStatusHistoryConfiguration : IEntityTypeConfiguration<CardStatusHistory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CardStatusHistory> builder)
    {
        builder.ToTable("CardStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.CardId)
            .IsRequired();

        builder.Property(h => h.PreviousStatusId)
            .IsRequired(false);

        builder.Property(h => h.NewStatusId)
            .IsRequired();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        // MaxLength matches the standard ASP.NET Core Identity user ID column length.
        builder.Property(h => h.ChangedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.ChangeSource)
            .IsRequired()
            .HasConversion<int>();

        // FK → BoardStatus (previous status): restrict deletion of a status that is
        // still referenced by history records. Nullable because the initial creation
        // record has no previous status.
        builder.HasOne<BoardStatus>()
            .WithMany()
            .HasForeignKey(h => h.PreviousStatusId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // FK → BoardStatus (new status): restrict deletion of a status that is
        // still referenced by history records.
        builder.HasOne<BoardStatus>()
            .WithMany()
            .HasForeignKey(h => h.NewStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on CardId for fast history lookups by card.
        builder.HasIndex(h => h.CardId);

        // Index on ChangedAt for efficient chronological ordering.
        builder.HasIndex(h => h.ChangedAt);
    }
}
