using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="CardTag"/> join entity.
/// Maps the entity to the <c>CardTags</c> table with a composite primary key on
/// (<see cref="CardTag.CardId"/>, <see cref="CardTag.TagId"/>) — no separate <c>Guid Id</c> column.
/// </summary>
public sealed class CardTagConfiguration : IEntityTypeConfiguration<CardTag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CardTag> builder)
    {
        builder.ToTable("CardTags");

        // Composite PK — no separate Guid Id.
        builder.HasKey(ct => new { ct.CardId, ct.TagId });

        builder.Property(ct => ct.CardId)
            .IsRequired();

        builder.Property(ct => ct.TagId)
            .IsRequired();

        // FK → Card: cascade-delete CardTag join records when the owning card is removed.
        // WithMany uses the backing-field collection Card._tags exposed as Card.Tags.
        builder.HasOne(ct => ct.Card)
            .WithMany(c => c.Tags)
            .HasForeignKey(ct => ct.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → ProjectTag: cascade-delete CardTag join records when the tag is removed.
        builder.HasOne(ct => ct.Tag)
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
