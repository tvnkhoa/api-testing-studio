using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiTestingStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultBody",
                table: "Endpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultHeaders",
                table: "Endpoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EndpointId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMs = table.Column<long>(type: "INTEGER", nullable: false),
                    DnsMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ConnectMs = table.Column<long>(type: "INTEGER", nullable: true),
                    TimeToFirstByteMs = table.Column<long>(type: "INTEGER", nullable: true),
                    RequestSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_EndpointId",
                table: "RequestHistory",
                column: "EndpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestHistory");

            migrationBuilder.DropColumn(
                name: "DefaultBody",
                table: "Endpoints");

            migrationBuilder.DropColumn(
                name: "DefaultHeaders",
                table: "Endpoints");
        }
    }
}
