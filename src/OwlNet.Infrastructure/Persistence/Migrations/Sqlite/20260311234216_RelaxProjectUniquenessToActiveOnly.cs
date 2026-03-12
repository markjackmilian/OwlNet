using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OwlNet.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RelaxProjectUniquenessToActiveOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — this migration was applied before the DropIndex
            // commands were added. The corrective migration FixDropGlobalProjectIndexes
            // handles the actual index removal for SQLite.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to revert — see FixDropGlobalProjectIndexes.
        }
    }
}
