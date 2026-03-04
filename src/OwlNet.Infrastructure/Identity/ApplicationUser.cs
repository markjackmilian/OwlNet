using Microsoft.AspNetCore.Identity;

namespace OwlNet.Infrastructure.Identity;

/// <summary>
/// Represents the application user entity for ASP.NET Core Identity.
/// Extends <see cref="IdentityUser"/> to provide an extensibility point
/// for adding custom user properties in the future.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
}
