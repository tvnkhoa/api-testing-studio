using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestsAndAssertions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "WorkflowId",
                table: "TestCases",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "EndpointId",
                table: "TestCases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "TestCases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TestSuiteId",
                table: "TestCases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Assertions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Target = table.Column<string>(type: "TEXT", nullable: true),
                    Expression = table.Column<string>(type: "TEXT", nullable: true),
                    Operator = table.Column<string>(type: "TEXT", nullable: true),
                    Expected = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assertions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestSuiteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PassedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestSuites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSuites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_TestSuiteId",
                table: "TestCases",
                column: "TestSuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_WorkspaceId",
                table: "TestCases",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Assertions_TestCaseId",
                table: "Assertions",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestCaseId",
                table: "TestResults",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_WorkspaceId",
                table: "TestResults",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TestSuites_WorkspaceId",
                table: "TestSuites",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assertions");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestSuites");

            migrationBuilder.DropIndex(
                name: "IX_TestCases_TestSuiteId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "IX_TestCases_WorkspaceId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "EndpointId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "TestSuiteId",
                table: "TestCases");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkflowId",
                table: "TestCases",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
