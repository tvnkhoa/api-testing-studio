using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "RunSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "RunSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Iteration",
                table: "RunSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "RunSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentStepId",
                table: "RunSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestSnapshot",
                table: "RunSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseSnapshot",
                table: "RunSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedUtc",
                table: "RunSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "RunSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "Runs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Runs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Runs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetId",
                table: "Runs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetName",
                table: "Runs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LogEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_ParentStepId",
                table: "RunSteps",
                column: "ParentStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_RunId",
                table: "RunSteps",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_WorkspaceId",
                table: "Runs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEvents_WorkspaceId",
                table: "LogEvents",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEvents");

            migrationBuilder.DropIndex(
                name: "IX_RunSteps_ParentStepId",
                table: "RunSteps");

            migrationBuilder.DropIndex(
                name: "IX_RunSteps_RunId",
                table: "RunSteps");

            migrationBuilder.DropIndex(
                name: "IX_Runs_WorkspaceId",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Iteration",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "ParentStepId",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "RequestSnapshot",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "ResponseSnapshot",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "StartedUtc",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "TargetName",
                table: "Runs");
        }
    }
}
