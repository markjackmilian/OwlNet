using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Card"/> entity.
/// Maps the entity to the <c>Cards</c> table and enforces a composite unique index
/// on (<see cref="Card.ProjectId"/>, <see cref="Card.Number"/>) to guarantee that
/// card numbers are unique within each project.
/// </summary>
public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Number)
            .IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Description: max 5000 chars enforced at application level (FluentValidation + domain entity).
        // SQL Server does not support nvarchar(N) for N > 4000 in fixed-length columns;
        // nvarchar(max) is used intentionally. The 5000-char limit is enforced by the application layer.
        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(5000)
            .HasDefaultValue(string.Empty);

        builder.Property(c => c.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.StatusId)
            .IsRequired();

        builder.Property(c => c.ProjectId)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Unique card number per project — enforced at the database level.
        builder.HasIndex(c => new { c.ProjectId, c.Number })
            .IsUnique();

        // FK → Project: cascade-delete cards when the owning project is removed.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → BoardStatus (current status): restrict deletion of a status that is
        // still referenced by one or more cards.
        builder.HasOne<BoardStatus>()
            .WithMany()
            .HasForeignKey(c => c.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: cascade-delete history records when the owning card is removed.
        builder.HasMany(c => c.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
