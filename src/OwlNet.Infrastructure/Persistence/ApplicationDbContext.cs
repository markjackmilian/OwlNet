using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OwlNet.Domain.Entities;
using OwlNet.Infrastructure.Identity;

namespace OwlNet.Infrastructure.Persistence;

/// <summary>
/// The application database context that integrates Entity Framework Core with ASP.NET Core Identity.
/// Inherits from <see cref="IdentityDbContext{TUser, TRole, TKey}"/> to provide Identity table support
/// and applies entity configurations from the Infrastructure assembly.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the set of <see cref="AppSetting"/> entities representing global application settings.
    /// </summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>
    /// Gets the set of <see cref="Project"/> entities representing projects.
    /// </summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>
    /// Configures the model by applying Identity table mappings and all
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> implementations from the Infrastructure assembly.
    /// </summary>
    /// <param name="builder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
