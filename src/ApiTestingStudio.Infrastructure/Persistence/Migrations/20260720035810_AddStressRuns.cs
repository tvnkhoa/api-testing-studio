using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStressRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StressMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StressRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ElapsedMs = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestCount = table.Column<long>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestsPerSecond = table.Column<double>(type: "REAL", nullable: false),
                    MinMs = table.Column<double>(type: "REAL", nullable: false),
                    AverageMs = table.Column<double>(type: "REAL", nullable: false),
                    MaxMs = table.Column<double>(type: "REAL", nullable: false),
                    P50Ms = table.Column<double>(type: "REAL", nullable: false),
                    P95Ms = table.Column<double>(type: "REAL", nullable: false),
                    P99Ms = table.Column<double>(type: "REAL", nullable: false),
                    ErrorRate = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StressMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StressRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetKind = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TargetName = table.Column<string>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    VirtualUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    Iterations = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    WarmupIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    Cancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RequestCount = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestsPerSecond = table.Column<double>(type: "REAL", nullable: false),
                    P95Ms = table.Column<double>(type: "REAL", nullable: false),
                    ErrorRate = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StressRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StressMetrics_StressRunId",
                table: "StressMetrics",
                column: "StressRunId");

            migrationBuilder.CreateIndex(
                name: "IX_StressRuns_WorkspaceId",
                table: "StressRuns",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StressMetrics");

            migrationBuilder.DropTable(
                name: "StressRuns");
        }
    }
}
