using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneySharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AcceptedConnectionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAt",
                table: "Connections",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.Sql("UPDATE \"Connections\" SET \"AcceptedAt\" = \"UpdatedAt\" WHERE \"Status\" IN (1, 4, 5, 6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Connections");
        }
    }
}
