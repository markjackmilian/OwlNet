using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OwlNet.Application.Common.Interfaces;
using OwlNet.Infrastructure.Identity;
using OwlNet.Infrastructure.Persistence;
using OwlNet.Infrastructure.Persistence.Repositories;
using OwlNet.Infrastructure.Services;

namespace OwlNet.Infrastructure;

/// <summary>
/// Provides extension methods for registering Infrastructure layer services
/// into the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the specified <see cref="IServiceCollection"/>.
    /// Configures Entity Framework Core with the appropriate database provider (SQL Server or SQLite)
    /// based on the <c>DatabaseProvider</c> configuration setting, and registers ASP.NET Core Identity.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration containing connection strings and provider settings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>DatabaseProvider</c> configuration value is not <c>"SqlServer"</c> or <c>"Sqlite"</c>.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider");
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            _ = databaseProvider switch
            {
                "SqlServer" => options.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)),

                "Sqlite" => options.UseSqlite(
                    connectionString,
                    sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)),

                _ => throw new InvalidOperationException(
                    $"Unsupported database provider: '{databaseProvider}'. Supported values are 'SqlServer' and 'Sqlite'.")
            };
        });

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddSingleton<ICliService, CliService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IFileSystem, FileSystemService>();

        services.AddHttpClient<ILlmProviderService, LlmProviderService>(client =>
        {
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
