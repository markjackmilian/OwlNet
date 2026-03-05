using DispatchR.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OwlNet.Application;
using OwlNet.Infrastructure;
using OwlNet.Infrastructure.Identity;
using OwlNet.Infrastructure.Persistence;
using OwlNet.Web.Components;
using OwlNet.Web.Components.Account;
using Serilog;

// ---------------------------------------------------------------------------
// Serilog Bootstrap
// ---------------------------------------------------------------------------
// Create an early logger so that startup errors are captured before the host
// is fully built. This is replaced by the configuration-driven logger below.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OwlNet web application");

    var builder = WebApplication.CreateBuilder(args);

    // -----------------------------------------------------------------------
    // Serilog — replace the default ASP.NET Core logging with Serilog.
    // Full configuration (sinks, enrichers, minimum levels) is read from
    // appsettings.json / appsettings.{Environment}.json so that operators
    // can tune logging without recompilation.
    // -----------------------------------------------------------------------
    builder.Services.AddSerilog(
        (services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services));

    // -----------------------------------------------------------------------
    // Application & Infrastructure layers (Clean Architecture DI extensions)
    // -----------------------------------------------------------------------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // -----------------------------------------------------------------------
    // DispatchR — CQRS mediator pipeline (registered in composition root)
    // Discovers handlers, pipeline behaviors, and notification handlers
    // from the Application assembly.
    // -----------------------------------------------------------------------
    builder.Services.AddDispatchR(
        typeof(OwlNet.Application.DependencyInjection).Assembly,
        withPipelines: true,
        withNotifications: true);

    // -----------------------------------------------------------------------
    // Blazor Server — Razor components with interactive server-side rendering
    // -----------------------------------------------------------------------
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // -----------------------------------------------------------------------
    // MudBlazor — Material Design component library
    // -----------------------------------------------------------------------
    builder.Services.AddMudServices();

    // -----------------------------------------------------------------------
    // ASP.NET Core Identity — authentication & authorization scaffolding
    // -----------------------------------------------------------------------
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

    builder.Services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });

    // Fallback authorization policy — require authenticated users globally.
    // Every page and endpoint must have an authenticated user unless explicitly
    // opted out with [AllowAnonymous] (e.g., Account login/register pages).
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

    // -----------------------------------------------------------------------
    // Build the application
    // -----------------------------------------------------------------------
    var app = builder.Build();

    // -----------------------------------------------------------------------
    // Automatic Database Migration
    // -----------------------------------------------------------------------
    // Apply any pending EF Core migrations at startup to ensure the database
    // schema is always up-to-date. This eliminates the need for manual
    // `dotnet ef database update` commands during development and deployment.
    Log.Information("Applying database migrations...");

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    Log.Information("Database migrations applied successfully");

    // -----------------------------------------------------------------------
    // HTTP Request Pipeline
    // -----------------------------------------------------------------------
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();

    // Serilog request logging — emits a single structured log event per HTTP
    // request with timing, status code, and path information.
    app.UseSerilogRequestLogging();

    // -----------------------------------------------------------------------
    // Authentication & Authorization — enforce the auth gate globally.
    // Account pages opt out via [AllowAnonymous] in their _Imports.razor.
    // -----------------------------------------------------------------------
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets().AllowAnonymous();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Identity account endpoints (login, logout, external auth callbacks, etc.)
    app.MapAdditionalIdentityEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Ensure all buffered log events are flushed to sinks before process exit
    Log.CloseAndFlush();
}
