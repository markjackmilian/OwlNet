---
name: ef-migrations
description: Generate EF Core migrations for the OwlNet dual-provider setup (SqlServer + Sqlite). Covers the correct build, generate, and snapshot workflow to avoid name collisions and snapshot overwrites.
metadata:
  audience: developers
  workflow: ef-core
---

## What I do

Guide the generation of EF Core migrations for **both SqlServer and Sqlite** providers in the OwlNet project, which keeps both sets of migrations in the same assembly (`OwlNet.Infrastructure`) using conditional compilation.

## Architecture

- **DbContext**: `ApplicationDbContext` in `src/OwlNet.Infrastructure/Persistence/`
- **Design-time factory**: `DesignTimeDbContextFactory` reads the provider from CLI args (`-- SqlServer`) or the `EF_PROVIDER` environment variable. Defaults to Sqlite.
- **Conditional compilation**: `OwlNet.Infrastructure.csproj` reads `$(EF_PROVIDER)` and excludes the OTHER provider's migration files from compilation to prevent `[Migration]` name and `ModelSnapshot` class collisions.
- **Migration output directories**:
  - `Persistence/Migrations/SqlServer/`
  - `Persistence/Migrations/Sqlite/`

## How to generate migrations

### Step 1 — Build explicitly with the target provider

EF Core scans the compiled assembly for `[Migration]` attributes. You MUST build with `EF_PROVIDER` set so that only the target provider's migration files are compiled:

```bash
# For Sqlite
export EF_PROVIDER=Sqlite && dotnet build OwlNet.sln --no-incremental --no-restore

# For SqlServer
export EF_PROVIDER=SqlServer && dotnet build OwlNet.sln --no-incremental --no-restore
```

`--no-incremental` is required because MSBuild may cache a previous build that included the other provider's files.

### Step 2 — Generate the migration with `--no-build`

Use `--no-build` to ensure `dotnet ef` uses the assembly you just built (and doesn't rebuild without the env var):

```bash
# Sqlite
export EF_PROVIDER=Sqlite && dotnet ef migrations add <MigrationName> \
  --project src/OwlNet.Infrastructure \
  --startup-project src/OwlNet.Web \
  --output-dir Persistence/Migrations/Sqlite \
  --no-build \
  -- Sqlite

# SqlServer
export EF_PROVIDER=SqlServer && dotnet ef migrations add <MigrationName> \
  --project src/OwlNet.Infrastructure \
  --startup-project src/OwlNet.Web \
  --output-dir Persistence/Migrations/SqlServer \
  --no-build \
  -- SqlServer
```

### Step 3 — Verify the generated files

Each provider folder must contain:
- `<timestamp>_<MigrationName>.cs` — the migration Up/Down methods
- `<timestamp>_<MigrationName>.Designer.cs` — model metadata
- `ApplicationDbContextModelSnapshot.cs` — current model snapshot

Verify column types are correct for each provider:
- **SqlServer**: `nvarchar(...)`, `bit`, `int`, `datetimeoffset`
- **Sqlite**: `TEXT`, `INTEGER`

## Critical: snapshot overwrite problem

EF Core locates the `ApplicationDbContextModelSnapshot.cs` file **on the filesystem** (not just in the compiled assembly). When generating for the second provider, it may find the first provider's snapshot file and **overwrite it** with the second provider's model.

### How to avoid this

Generate migrations for **both providers** using this exact sequence:

1. **Clean start**: remove all `*.cs` files from both `Migrations/SqlServer/` and `Migrations/Sqlite/` (keep `.gitkeep`).
2. **Generate Sqlite first** (the default provider):
   ```bash
   export EF_PROVIDER=Sqlite
   dotnet build OwlNet.sln --no-incremental --no-restore
   dotnet ef migrations add <Name> --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/Sqlite --no-build -- Sqlite
   ```
   This creates the migration + snapshot in `Migrations/Sqlite/`.
3. **Generate SqlServer second**:
   ```bash
   export EF_PROVIDER=SqlServer
   dotnet build OwlNet.sln --no-incremental --no-restore
   dotnet ef migrations add <Name> --project src/OwlNet.Infrastructure --startup-project src/OwlNet.Web --output-dir Persistence/Migrations/SqlServer --no-build -- SqlServer
   ```
4. **Check the Sqlite snapshot**: after generating SqlServer, verify that `Migrations/Sqlite/ApplicationDbContextModelSnapshot.cs` still has `TEXT`/`INTEGER` types (not `nvarchar`). If it was overwritten, restore it from the Sqlite Designer file (copy `BuildTargetModel` content into `BuildModel`).

### If the snapshot was overwritten

The snapshot content is identical to the Designer file's `BuildTargetModel` method, but renamed to `BuildModel`. You can manually recreate it:

1. Open the Designer file for the affected provider (e.g., `Sqlite/<timestamp>_InitialCreate.Designer.cs`)
2. Copy the class, change:
   - Class name from `InitialCreate` to `ApplicationDbContextModelSnapshot`
   - Base class from (none) to `: ModelSnapshot`
   - Method name from `BuildTargetModel` to `BuildModel`
   - Remove the `[Migration("...")]` attribute (keep `[DbContext(...)]`)
3. Save as `ApplicationDbContextModelSnapshot.cs` in the correct provider folder

## When to use me

Use this skill whenever you need to:
- Add a new EF Core migration after modifying entities or configurations
- Regenerate migrations from scratch
- Troubleshoot migration name collisions or snapshot corruption
- Understand the dual-provider migration architecture in OwlNet
