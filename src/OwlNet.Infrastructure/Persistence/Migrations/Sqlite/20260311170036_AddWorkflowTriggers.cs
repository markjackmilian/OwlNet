using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OwlNet.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddWorkflowTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowTriggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FromStatusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToStatusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Prompt = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTriggers_BoardStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "BoardStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTriggers_BoardStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "BoardStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTriggers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTriggerAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowTriggerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggerAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTriggerAgents_WorkflowTriggers_WorkflowTriggerId",
                        column: x => x.WorkflowTriggerId,
                        principalTable: "WorkflowTriggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggerAgents_WorkflowTriggerId",
                table: "WorkflowTriggerAgents",
                column: "WorkflowTriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggers_FromStatusId",
                table: "WorkflowTriggers",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggers_ProjectId",
                table: "WorkflowTriggers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggers_ProjectId_FromStatusId_ToStatusId",
                table: "WorkflowTriggers",
                columns: new[] { "ProjectId", "FromStatusId", "ToStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggers_ToStatusId",
                table: "WorkflowTriggers",
                column: "ToStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowTriggerAgents");

            migrationBuilder.DropTable(
                name: "WorkflowTriggers");
        }
    }
}
