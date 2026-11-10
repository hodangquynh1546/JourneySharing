using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneySharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublicLocationAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationArea",
                table: "Trips",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginArea",
                table: "Trips",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinationArea",
                table: "TravelRequests",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginArea",
                table: "TravelRequests",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationArea",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OriginArea",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DestinationArea",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "OriginArea",
                table: "TravelRequests");
        }
    }
}
