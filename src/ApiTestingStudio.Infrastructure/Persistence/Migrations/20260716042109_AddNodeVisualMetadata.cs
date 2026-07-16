using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeVisualMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "WorkflowNodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "WorkflowNodes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "WorkflowNodes",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "WorkflowNodes");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "WorkflowNodes");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "WorkflowNodes");
        }
    }
}
