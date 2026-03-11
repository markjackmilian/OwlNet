using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OwlNet.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddBoardCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false, defaultValue: ""),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_BoardStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "BoardStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cards_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousStatusId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NewStatusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ChangeSource = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardStatusHistories_BoardStatuses_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "BoardStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardStatusHistories_BoardStatuses_PreviousStatusId",
                        column: x => x.PreviousStatusId,
                        principalTable: "BoardStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardStatusHistories_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ProjectId_Number",
                table: "Cards",
                columns: new[] { "ProjectId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_StatusId",
                table: "Cards",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CardStatusHistories_CardId",
                table: "CardStatusHistories",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardStatusHistories_ChangedAt",
                table: "CardStatusHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CardStatusHistories_NewStatusId",
                table: "CardStatusHistories",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CardStatusHistories_PreviousStatusId",
                table: "CardStatusHistories",
                column: "PreviousStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CardStatusHistories");
            migrationBuilder.DropTable(name: "Cards");
        }
    }
}
