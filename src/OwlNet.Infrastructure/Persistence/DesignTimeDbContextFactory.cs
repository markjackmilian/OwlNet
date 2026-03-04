using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OwlNet.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> CLI tools to create an
/// <see cref="ApplicationDbContext"/> instance for generating migrations.
/// <para>
/// The factory determines the database provider from two sources (in priority order):
/// <list type="number">
///   <item><description>CLI arguments passed after <c>--</c> (e.g., <c>-- SqlServer</c>).</description></item>
///   <item><description>The <c>EF_PROVIDER</c> environment variable.</description></item>
/// </list>
/// Supported values: <c>"SqlServer"</c> and <c>"Sqlite"</c>. Defaults to SQLite when neither source is set.
/// </para>
/// <para>
/// Usage examples:
/// <code>
/// # SQLite migrations (default — no extra args needed)
/// dotnet ef migrations add InitialCreate --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/Sqlite
///
/// # SQL Server migrations (via CLI passthrough argument)
/// dotnet ef migrations add InitialCreate --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/SqlServer -- SqlServer
///
/// # SQL Server migrations (via environment variable)
/// set EF_PROVIDER=SqlServer
/// dotnet ef migrations add InitialCreate --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/SqlServer
/// </code>
/// </para>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string SqlServerProvider = "SqlServer";
    private const string SqliteProvider = "Sqlite";

    private const string SqlServerConnectionString =
        "Server=.;Database=OwlNet;Trusted_Connection=True;TrustServerCertificate=True";

    private const string SqliteConnectionString = "Data Source=OwlNet.db";

    /// <summary>
    /// Creates a new <see cref="ApplicationDbContext"/> configured for the database provider
    /// specified by the CLI arguments (after <c>--</c>) or the <c>EF_PROVIDER</c> environment variable.
    /// Defaults to SQLite when neither source is set.
    /// </summary>
    /// <param name="args">
    /// Arguments provided by the design-time tools. When using <c>dotnet ef</c>,
    /// pass the provider name after <c>--</c> (e.g., <c>-- SqlServer</c>).
    /// </param>
    /// <returns>A configured <see cref="ApplicationDbContext"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resolved provider value is not <c>"SqlServer"</c> or <c>"Sqlite"</c>.
    /// </exception>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Priority: CLI args > environment variable > default (Sqlite)
        var provider = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("EF_PROVIDER");

        var migrationsAssembly = typeof(ApplicationDbContext).Assembly.FullName;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        _ = provider switch
        {
            SqlServerProvider => optionsBuilder.UseSqlServer(
                SqlServerConnectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)),

            SqliteProvider or null or "" => optionsBuilder.UseSqlite(
                SqliteConnectionString,
                sqliteOptions => sqliteOptions.MigrationsAssembly(migrationsAssembly)),

            _ => throw new InvalidOperationException(
                $"Unsupported database provider: '{provider}'. Supported values are '{SqlServerProvider}' and '{SqliteProvider}'.")
        };

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
