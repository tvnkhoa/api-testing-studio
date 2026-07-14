using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCatalogHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Endpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Endpoints",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EndpointFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndpointFolders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_WorkspaceId",
                table: "Services",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Endpoints_FolderId",
                table: "Endpoints",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Endpoints_ServiceId",
                table: "Endpoints",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EndpointFolders_ParentFolderId",
                table: "EndpointFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_EndpointFolders_ServiceId",
                table: "EndpointFolders",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EndpointFolders");

            migrationBuilder.DropIndex(
                name: "IX_Services_WorkspaceId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Endpoints_FolderId",
                table: "Endpoints");

            migrationBuilder.DropIndex(
                name: "IX_Endpoints_ServiceId",
                table: "Endpoints");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Endpoints");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Endpoints");
        }
    }
}
