using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace OwlNet.Application;

/// <summary>
/// Provides extension methods for registering Application layer services
/// into the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services to the specified <see cref="IServiceCollection"/>.
    /// Registers FluentValidation validators from the Application assembly.
    /// DispatchR mediator registration is handled in the composition root (Web project)
    /// to keep the Application layer free of concrete implementation dependencies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
