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
    /// Gets the set of <see cref="SystemAgent"/> entities representing system-wide agent definitions.
    /// </summary>
    public DbSet<SystemAgent> SystemAgents => Set<SystemAgent>();

    /// <summary>
    /// Gets the set of <see cref="BoardStatus"/> entities representing board column statuses.
    /// </summary>
    public DbSet<BoardStatus> BoardStatuses => Set<BoardStatus>();

    /// <summary>
    /// Gets the set of <see cref="Card"/> entities representing Kanban board cards.
    /// </summary>
    public DbSet<Card> Cards => Set<Card>();

    /// <summary>
    /// Gets the set of <see cref="CardStatusHistory"/> entities representing card status transitions.
    /// </summary>
    public DbSet<CardStatusHistory> CardStatusHistories => Set<CardStatusHistory>();

    /// <summary>
    /// Gets the set of <see cref="ProjectTag"/> entities representing project-scoped tag vocabulary.
    /// </summary>
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();

    /// <summary>
    /// Gets the set of <see cref="CardTag"/> entities representing card-tag associations.
    /// </summary>
    public DbSet<CardTag> CardTags => Set<CardTag>();

    /// <summary>
    /// Gets the set of <see cref="CardComment"/> entities representing comments posted on cards.
    /// </summary>
    public DbSet<CardComment> CardComments => Set<CardComment>();

    /// <summary>
    /// Gets the set of <see cref="WorkflowTrigger"/> entities representing workflow automation triggers.
    /// </summary>
    public DbSet<WorkflowTrigger> WorkflowTriggers => Set<WorkflowTrigger>();

    /// <summary>
    /// Gets the set of <see cref="WorkflowTriggerAgent"/> entities representing the ordered agent associations for workflow triggers.
    /// </summary>
    public DbSet<WorkflowTriggerAgent> WorkflowTriggerAgents => Set<WorkflowTriggerAgent>();

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
