using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileAuthAndEnvironmentBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnvironmentId",
                table: "Variables",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHeaderName",
                table: "Profiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Auth",
                table: "Profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_EnvironmentId",
                table: "Variables",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_WorkspaceId",
                table: "Variables",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_WorkspaceId",
                table: "Profiles",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_WorkspaceId",
                table: "Environments",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variables_EnvironmentId",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Variables_WorkspaceId",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_WorkspaceId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Environments_WorkspaceId",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ApiKeyHeaderName",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Auth",
                table: "Profiles");
        }
    }
}
