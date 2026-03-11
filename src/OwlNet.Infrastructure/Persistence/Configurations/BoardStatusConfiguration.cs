using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="BoardStatus"/> entity.
/// Maps the entity to the <c>BoardStatuses</c> table with a composite unique index
/// on (<see cref="BoardStatus.ProjectId"/>, <see cref="BoardStatus.Name"/>) to enforce
/// name uniqueness within each scope (global defaults and per-project).
/// </summary>
public sealed class BoardStatusConfiguration : IEntityTypeConfiguration<BoardStatus>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BoardStatus> builder)
    {
        builder.ToTable("BoardStatuses");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.SortOrder)
            .IsRequired();

        builder.Property(b => b.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.ProjectId)
            .IsRequired(false);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        builder.HasIndex(b => new { b.ProjectId, b.Name })
            .IsUnique();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
