using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Project"/> entity.
/// Maps the entity to the <c>Projects</c> table with filtered unique indexes on <see cref="Project.Name"/> and
/// <see cref="Project.Path"/> that apply only to active (non-archived) projects (<c>IsArchived = false</c>).
/// </summary>
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("[IsArchived] = 0");

        builder.Property(p => p.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(p => p.Path)
            .IsUnique()
            .HasFilter("[IsArchived] = 0");

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(p => p.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.IsFavorited)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
