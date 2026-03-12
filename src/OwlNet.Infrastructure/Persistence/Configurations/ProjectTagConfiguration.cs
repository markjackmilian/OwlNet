using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ProjectTag"/> entity.
/// Maps the entity to the <c>ProjectTags</c> table and enforces a composite unique index
/// on (<see cref="ProjectTag.ProjectId"/>, <see cref="ProjectTag.Name"/>) to guarantee that
/// tag names are unique within each project at the database level.
/// Case-insensitive uniqueness is enforced at the Application layer.
/// </summary>
public sealed class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectTag> builder)
    {
        builder.ToTable("ProjectTags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Color)
            .HasMaxLength(7);

        builder.Property(t => t.ProjectId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        // Unique tag name per project (binary uniqueness; case-insensitive enforced at app layer).
        builder.HasIndex(t => new { t.ProjectId, t.Name })
            .IsUnique();

        // FK → Project: cascade-delete tags when the owning project is removed.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
