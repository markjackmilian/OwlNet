using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Constants;
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
        services.AddScoped<IBoardStatusRepository, BoardStatusRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ISystemAgentRepository, SystemAgentRepository>();
        services.AddScoped<IWorkflowTriggerRepository, WorkflowTriggerRepository>();
        services.AddScoped<BoardStatusSeeder>();
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddSingleton<ICliService, CliService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IFileSystem, FileSystemService>();
        services.AddSingleton<IAgentFileService, AgentFileService>();

        services.AddHttpClient<ILlmProviderService, LlmProviderService>(client =>
        {
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<ILlmChatService, LlmChatService>(client =>
        {
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ── OpenCode Server integration ─────────────────────────────────────
        // Typed HTTP client for OpenCode Server API calls.
        // No BaseAddress — the URL is resolved at request time from IAppSettingService.
        services.AddHttpClient<IOpenCodeClient, OpenCodeClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(OpenCodeConstants.DefaultTimeoutSeconds);
        });

        // Typed HTTP client for OpenCode session management API calls.
        // No BaseAddress — the URL is resolved at request time.
        services.AddHttpClient<IOpenCodeSessionService, OpenCodeSessionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(OpenCodeConstants.DefaultTimeoutSeconds);
        });

        // Typed HTTP client for OpenCode messaging API calls (prompts, messages, commands).
        // Uses an extended timeout (300s) because AI prompt responses can take several minutes.
        // No BaseAddress — the URL is resolved at request time.
        services.AddHttpClient<IOpenCodeMessageService, OpenCodeMessageService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(OpenCodeConstants.MessageTimeoutSeconds);
        });

        // Singleton manager for the OpenCode Server process lifecycle.
        // Registered as singleton because it manages a single global process.
        services.AddSingleton<IOpenCodeServerManager, OpenCodeServerManager>();

        // Background service for auto-start and periodic health check polling.
        services.AddHostedService<OpenCodeServerHostedService>();

        // Named HTTP client for OpenCode Server SSE event streaming.
        // Uses infinite timeout because SSE connections are long-lived persistent streams.
        services.AddHttpClient("OpenCodeEventService", client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        // Singleton event service for OpenCode Server SSE event streaming.
        // Maintains a persistent SSE connection and distributes events to subscribers.
        services.AddSingleton<IOpenCodeEventService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("OpenCodeEventService");
            return new OpenCodeEventService(
                httpClient,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOpenCodeServerManager>(),
                sp.GetRequiredService<ILogger<OpenCodeEventService>>());
        });

        return services;
    }
}
