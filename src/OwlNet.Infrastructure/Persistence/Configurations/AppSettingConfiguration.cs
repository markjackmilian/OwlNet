using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OwlNet.Domain.Entities;

namespace OwlNet.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="AppSetting"/> entity.
/// Maps the entity to the <c>AppSettings</c> table with a unique index on <see cref="AppSetting.Key"/>.
/// </summary>
public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(s => s.Key)
            .IsUnique();

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();
    }
}
