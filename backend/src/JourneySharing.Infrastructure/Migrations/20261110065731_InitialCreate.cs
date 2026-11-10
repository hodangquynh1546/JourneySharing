using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace JourneySharing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    Destination = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    OriginAddress = table.Column<string>(type: "text", nullable: false),
                    DestinationAddress = table.Column<string>(type: "text", nullable: false),
                    DesiredTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false),
                    PreferredVehicleType = table.Column<string>(type: "text", nullable: true),
                    ProposedContribution = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelRequests", x => x.Id);
                    table.CheckConstraint("CK_Request_Contribution", "\"ProposedContribution\" IS NULL OR \"ProposedContribution\" >= 0");
                    table.CheckConstraint("CK_Request_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_Request_Passengers", "\"PassengerCount\" > 0");
                    table.ForeignKey(
                        name: "FK_TravelRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    LicensePlate = table.Column<string>(type: "text", nullable: true),
                    ShareableSeats = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.CheckConstraint("CK_Vehicle_Seats", "\"ShareableSeats\" > 0");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    Destination = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    OriginAddress = table.Column<string>(type: "text", nullable: false),
                    DestinationAddress = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<LineString>(type: "geometry(LineString,4326)", nullable: true),
                    DepartureTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false),
                    ContributionAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.CheckConstraint("CK_Trip_Contribution", "\"ContributionAmount\" IS NULL OR \"ContributionAmount\" >= 0");
                    table.CheckConstraint("CK_Trip_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_Trip_Passengers", "\"PassengerCount\" > 0");
                    table.CheckConstraint("CK_Trip_Seats", "(\"VehicleId\" IS NOT NULL AND \"AvailableSeats\" > 0) OR (\"VehicleId\" IS NULL AND \"AvailableSeats\" = 0)");
                    table.ForeignKey(
                        name: "FK_Trips_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtherTripId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedSeats = table.Column<int>(type: "integer", nullable: false),
                    AgreedContribution = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.CheckConstraint("CK_Connection_Contribution", "\"AgreedContribution\" IS NULL OR \"AgreedContribution\" >= 0");
                    table.CheckConstraint("CK_Connection_Counterpart", "(\"TravelRequestId\" IS NULL) <> (\"OtherTripId\" IS NULL)");
                    table.CheckConstraint("CK_Connection_DifferentTrip", "\"OtherTripId\" IS NULL OR \"OtherTripId\" <> \"TripId\"");
                    table.CheckConstraint("CK_Connection_Seats", "\"ReservedSeats\" > 0");
                    table.ForeignKey(
                        name: "FK_Connections_TravelRequests_TravelRequestId",
                        column: x => x.TravelRequestId,
                        principalTable: "TravelRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Trips_OtherTripId",
                        column: x => x.OtherTripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_OtherTripId",
                table: "Connections",
                column: "OtherTripId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_SenderId",
                table: "Connections",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TravelRequestId",
                table: "Connections",
                column: "TravelRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TripId_OtherTripId",
                table: "Connections",
                columns: new[] { "TripId", "OtherTripId" },
                unique: true,
                filter: "\"OtherTripId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TripId_TravelRequestId",
                table: "Connections",
                columns: new[] { "TripId", "TravelRequestId" },
                unique: true,
                filter: "\"TravelRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ExpiresAt",
                table: "TravelRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_Status_DesiredTime",
                table: "TravelRequests",
                columns: new[] { "Status", "DesiredTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_UserId",
                table: "TravelRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Destination",
                table: "Trips",
                column: "Destination")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ExpiresAt",
                table: "Trips",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Origin",
                table: "Trips",
                column: "Origin")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status_DepartureTime",
                table: "Trips",
                columns: new[] { "Status", "DepartureTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserId",
                table: "Trips",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_VehicleId",
                table: "Trips",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "TravelRequests");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
