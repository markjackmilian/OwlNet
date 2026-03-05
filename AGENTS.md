# AGENTS.md — OwlNet Coding Agent Guidelines

## Project Overview

OwlNet is an ASP.NET Core Blazor Server application (.NET 10.0) using Clean Architecture with four layers: Domain, Application, Infrastructure, and Web. UI is built with MudBlazor (Material Design). The mediator is **DispatchR** (not MediatR). EF Core supports dual providers (SQLite + SQL Server).

## Build / Run / Test Commands

```bash
# Build the full solution
dotnet build OwlNet.sln

# Run the web app (HTTP :5076, HTTPS :7114)
dotnet run --project src/OwlNet.Web

# Run ALL tests
dotnet test

# Run a single test by fully-qualified name
dotnet test --filter "FullyQualifiedName~MyClass.MyMethod"

# Run tests in one class
dotnet test --filter "ClassName=CreateOrderCommandHandlerTests"

# Run tests matching a name pattern
dotnet test --filter "MethodName~Handle_ValidCommand"

# Run tests in a namespace
dotnet test --filter "Namespace~OwlNet.Tests.Domain"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### EF Core Migrations (Dual Provider)

Always generate migrations for **both** providers when changing the model. See `.opencode/skills/ef-migrations/SKILL.md` for the full workflow.

```bash
# SQLite
set EF_PROVIDER=Sqlite
dotnet build OwlNet.sln --no-incremental --no-restore
dotnet ef migrations add <Name> --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/Sqlite --no-build -- Sqlite

# SQL Server
set EF_PROVIDER=SqlServer
dotnet build OwlNet.sln --no-incremental --no-restore
dotnet ef migrations add <Name> --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/SqlServer --no-build -- SqlServer
```

## Architecture Rules

- **Domain** — Entities, Value Objects, Events, Interfaces. ZERO external dependencies.
- **Application** — CQRS handlers, validators, DTOs. Depends on Domain + `DispatchR.Mediator.Abstractions` + FluentValidation.
- **Infrastructure** — EF Core, Identity, repository implementations. Depends on Domain + Application.
- **Web** — Composition root (Blazor Server + DI wiring). Depends on Application + Infrastructure.
- **Tests** — Single project `OwlNet.Tests` mirroring `src/` folder structure.

Each layer exposes an `Add{Layer}()` extension method on `IServiceCollection` for DI registration.

## Code Style & Conventions

### Global Build Settings (Directory.Build.props)

- `TreatWarningsAsErrors` is **enabled** — all warnings are build errors
- `Nullable` reference types are **enabled** globally
- `ImplicitUsings` is **enabled**
- Target framework: `net10.0`
- Central package versioning via `Directory.Packages.props`

### C# Conventions

- **File-scoped namespaces**: `namespace OwlNet.Domain;`
- **Sealed classes** by default unless explicitly designed for inheritance
- **Records** for DTOs, Value Objects, Events, Commands, Queries
- **One class per file**, file name matches class name
- **4-space indentation**, Allman-style braces
- **C# 12+ features**: collection expressions (`[]`), raw string literals, pattern matching
- **async/await** everywhere — never block with `.Result` or `.Wait()`
- **CancellationToken** passed through all async method signatures
- **XML documentation (`///`)** on all public APIs
- Methods under 30 lines; extract complex logic into named private methods

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Private fields | `_camelCase` | `_orderRepository` |
| Parameters | `camelCase` | `configuration` |
| Methods (public/private) | `PascalCase` | `ToggleDrawer()` |
| Constants/static readonly | `PascalCase` | `SqliteProvider` |
| CSS classes | `kebab-case` | `kanban-card-priority--important` |
| Test methods | `Method_Scenario_Expected` | `Handle_ValidRequest_ReturnsUser` |
| Test classes | `{ClassUnderTest}Tests` | `CreateOrderCommandHandlerTests` |

### Imports

- Implicit usings cover `System`, `System.Linq`, etc. — only add explicit `using` for non-implicit namespaces
- Global `@using` directives in `_Imports.razor` for Blazor components
- No `using static` unless it significantly improves readability

### Error Handling

- **Result pattern** for expected business failures — do not throw exceptions for business logic
- **Exceptions** only for truly unexpected/unrecoverable errors
- **ErrorBoundary** wrapping Blazor page content with user-friendly error UI + retry
- **`is not null`** pattern matching for null checks (not `!= null`)
- **Switch expressions** with discard `_` throwing `InvalidOperationException` for unhandled cases

### Structured Logging (Serilog)

- Inject `ILogger<T>` where `T` is the consuming class — never use non-generic `ILogger`
- **Structured placeholders**: `_logger.LogInformation("Order {OrderId} created", order.Id)`
- **NEVER** use string interpolation: `$"Order {order.Id}"` loses structured properties
- Log levels: Debug (diagnostics), Information (milestones), Warning (recoverable), Error (with exception)
- Never log sensitive data (passwords, tokens, PII)
- Use `NullLogger<T>.Instance` in tests

## DispatchR Mediator (NOT MediatR)

Key syntax differences from MediatR — these are critical:

```csharp
// Request: type itself is a generic param
public sealed class GetUserQuery : IRequest<GetUserQuery, ValueTask<UserDto>> { }

// Handler: uses ValueTask<T>, not Task<T>
public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, ValueTask<UserDto>> { ... }

// Pipeline: chain-of-responsibility with required NextPipeline property
public sealed class ValidationBehavior<TReq, TRes> : IPipelineBehavior<TReq, ValueTask<TRes>>
{
    public required IRequestHandler<TReq, ValueTask<TRes>> NextPipeline { get; set; }
}
```

- Always use `ValueTask<T>` (not `Task<T>`) for handlers
- One handler per file: `{RequestName}Handler.cs`
- Separate `Commands/` and `Queries/` folders
- `DispatchR.Mediator.Abstractions` in Application; `DispatchR.Mediator` only in Web/Composition Root

## Testing Guidelines

**Framework**: xUnit | **Mocking**: NSubstitute | **Assertions**: Shouldly (never `Assert.*`)

- **Arrange-Act-Assert** pattern in every test
- `async Task` test methods (never `async void` or `async ValueTask`)
- `CancellationToken.None` unless testing cancellation
- `NullLogger<T>.Instance` for logger dependencies
- `Substitute.For<T>()` on interfaces only
- For `ValueTask<T>` returns: `.Returns(new ValueTask<T>(value))`
- `ShouldSatisfyAllConditions()` for multi-property assertions (reports ALL failures)
- `Should.ThrowAsync<T>()` for async exception testing
- Test coverage: happy path, validation failures, edge cases, error scenarios, business rules

## Blazor / MudBlazor Frontend

- **MudBlazor components** over raw HTML for interactive UI
- **Smart/Dumb pattern**: Pages inject services (smart); child components take `[Parameter]` + `EventCallback` (dumb)
- Never mutate `[Parameter]` inside a child — notify parent via `EventCallback`
- Use `MudThemeProvider` with custom `OwlNetTheme` — never hardcode colors
- Material Design icons via `Icons.Material.Filled.*` — no inline SVGs
- Every async operation must show loading feedback (spinner, skeleton, disabled button)
- `ISnackbar` for toast notifications, `IDialogService` for modals
- Components under 150 lines of `@code`; extract to `.razor.cs` or services if larger
- Implement `IDisposable` to clean up subscriptions, timers, CancellationTokenSource
- Load data in `OnInitializedAsync`, not constructors
- Comment every `[Parameter]` property explaining its purpose

## EF Core

- Fluent API exclusively for entity configuration — **no data annotations**
- One `IEntityTypeConfiguration<T>` per entity, one per file
- `DateTimeOffset` for timestamps (not `DateTime`)
- `decimal(18,2)` for money fields (SQLite stores as TEXT — be aware)
- DbContext must be clean — no business logic
- Always create migrations for BOTH providers when changing the model

## Additional Agent Docs

Detailed agent guidelines live in `.opencode/agents/`:
- `owl-coder.md` — Backend .NET development rules
- `owl-blazor.md` — Blazor/MudBlazor frontend rules
- `owl-tester.md` — Testing patterns and conventions
- `owl-review.md` — Code review checklist and verdicts
