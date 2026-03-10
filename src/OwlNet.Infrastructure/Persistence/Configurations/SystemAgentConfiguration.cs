using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="SystemAgent"/> entity.
/// Maps the entity to the <c>SystemAgents</c> table with a unique index on <see cref="SystemAgent.Name"/>
/// and a <c>TEXT</c> column type for <see cref="SystemAgent.Content"/> to support large Markdown payloads.
/// </summary>
public sealed class SystemAgentConfiguration : IEntityTypeConfiguration<SystemAgent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SystemAgent> builder)
    {
        builder.ToTable("SystemAgents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.Name)
            .IsUnique();

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Mode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();
    }
}
