using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JourneySharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NearbySearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_Destination",
                table: "TravelRequests",
                column: "Destination")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_Origin",
                table: "TravelRequests",
                column: "Origin")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_Destination",
                table: "TravelRequests");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_Origin",
                table: "TravelRequests");
        }
    }
}
