using System.Security.Claims;
using JourneySharing.Domain;
using JourneySharing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        api.MapPost("/vehicles", CreateVehicle).RequireAuthorization();
        api.MapGet("/vehicles/me", MyVehicles).RequireAuthorization();
        api.MapPost("/trips", CreateTrip).RequireAuthorization();
        api.MapGet("/trips", ListTrips);
        api.MapPost("/travel-requests", CreateRequest).RequireAuthorization();
        api.MapGet("/travel-requests", ListRequests);
    }

    private static Guid? UserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static async Task<IResult> CreateVehicle(VehicleInput input, ClaimsPrincipal principal, JourneyDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Type) || input.Type.Length > 80 ||
            input.ShareableSeats is < 1 or > 12)
            return Results.BadRequest(new { error = "Invalid vehicle type or seat count." });
        var vehicle = new Vehicle
        {
            UserId = userId.Value, Type = input.Type.Trim(), Model = input.Model?.Trim(),
            Color = input.Color?.Trim(), LicensePlate = input.LicensePlate?.Trim(),
            ShareableSeats = input.ShareableSeats
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/vehicles/{vehicle.Id}", new
        {
            vehicle.Id, vehicle.Type, vehicle.Model, vehicle.Color, vehicle.ShareableSeats
        });
    }

    private static async Task<IResult> MyVehicles(ClaimsPrincipal principal, JourneyDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        return Results.Ok(await db.Vehicles.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.Type, x.Model, x.Color, x.ShareableSeats }).ToListAsync());
    }

    private static async Task<IResult> CreateTrip(TripInput input, ClaimsPrincipal principal, JourneyDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (!ValidPost(input.Origin, input.Destination, input.OriginArea, input.DestinationArea,
                input.DepartureTime, input.ExpiresAt) ||
            input.PassengerCount is < 1 or > 12 || input.ContributionAmount < 0)
            return Results.BadRequest(new { error = "Invalid journey details." });
        if (input.VehicleId is null && input.AvailableSeats != 0)
            return Results.BadRequest(new { error = "A journey without a vehicle cannot offer seats." });
        if (input.VehicleId is not null)
        {
            var vehicle = await db.Vehicles.FindAsync(input.VehicleId);
            if (vehicle is null || vehicle.UserId != userId || input.AvailableSeats < 1 ||
                input.AvailableSeats > vehicle.ShareableSeats)
                return Results.BadRequest(new { error = "Vehicle or seat count is invalid." });
        }
        var trip = new Trip
        {
            UserId = userId.Value, VehicleId = input.VehicleId,
            Origin = Point(input.Origin), Destination = Point(input.Destination),
            OriginAddress = input.Origin.Address.Trim(), DestinationAddress = input.Destination.Address.Trim(),
            OriginArea = input.OriginArea.Trim(), DestinationArea = input.DestinationArea.Trim(),
            DepartureTime = input.DepartureTime, ExpiresAt = input.ExpiresAt,
            AvailableSeats = input.AvailableSeats, PassengerCount = input.PassengerCount,
            ContributionAmount = input.ContributionAmount
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/trips/{trip.Id}", PublicTrip(trip));
    }

    private static async Task<IResult> ListTrips(JourneyDbContext db)
    {
        var trips = await db.Trips.AsNoTracking()
            .Where(x => x.Status == TripStatus.Open && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderBy(x => x.DepartureTime).Take(50).ToListAsync();
        var ids = trips.Select(x => x.Id).ToArray();
        var occupied = await db.Connections.AsNoTracking()
            .Where(x => ids.Contains(x.TripId) &&
                (x.Status == ConnectionStatus.Accepted || x.Status == ConnectionStatus.InProgress))
            .GroupBy(x => x.TripId)
            .Select(x => new { TripId = x.Key, Seats = x.Sum(c => c.ReservedSeats) })
            .ToDictionaryAsync(x => x.TripId, x => x.Seats);
        return Results.Ok(trips.Select(x => PublicTrip(x, occupied.GetValueOrDefault(x.Id))));
    }

    private static object PublicTrip(Trip x, int occupiedSeats = 0) => new
    {
        x.Id, x.OriginArea, x.DestinationArea, x.DepartureTime, x.ExpiresAt,
        RemainingSeats = Math.Max(0, x.AvailableSeats - occupiedSeats),
        x.PassengerCount, HasVehicle = x.VehicleId is not null,
        x.ContributionAmount
    };

    private static async Task<IResult> CreateRequest(RequestInput input, ClaimsPrincipal principal, JourneyDbContext db)
    {
        var userId = UserId(principal);
        if (userId is null) return Results.Unauthorized();
        if (!ValidPost(input.Origin, input.Destination, input.OriginArea, input.DestinationArea,
                input.DesiredTime, input.ExpiresAt) ||
            input.PassengerCount is < 1 or > 12 || input.ProposedContribution < 0)
            return Results.BadRequest(new { error = "Invalid travel request." });
        var request = new TravelRequest
        {
            UserId = userId.Value, Origin = Point(input.Origin), Destination = Point(input.Destination),
            OriginAddress = input.Origin.Address.Trim(), DestinationAddress = input.Destination.Address.Trim(),
            OriginArea = input.OriginArea.Trim(), DestinationArea = input.DestinationArea.Trim(),
            DesiredTime = input.DesiredTime, ExpiresAt = input.ExpiresAt,
            PassengerCount = input.PassengerCount, PreferredVehicleType = input.PreferredVehicleType?.Trim(),
            ProposedContribution = input.ProposedContribution
        };
        db.TravelRequests.Add(request);
        await db.SaveChangesAsync();
        return Results.Created($"/api/v1/travel-requests/{request.Id}", PublicRequest(request));
    }

    private static async Task<IResult> ListRequests(JourneyDbContext db) => Results.Ok(
        (await db.TravelRequests.AsNoTracking()
            .Where(x => x.Status == RequestStatus.Open && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderBy(x => x.DesiredTime).Take(50).ToListAsync()).Select(PublicRequest));

    private static object PublicRequest(TravelRequest x) => new
    {
        x.Id, x.OriginArea, x.DestinationArea, x.DesiredTime, x.ExpiresAt,
        x.PassengerCount, x.PreferredVehicleType, x.ProposedContribution
    };

    private static bool ValidPost(LocationInput origin, LocationInput destination, string originArea,
        string destinationArea, DateTimeOffset departure, DateTimeOffset expiry) =>
        ValidLocation(origin) && ValidLocation(destination) &&
        !string.IsNullOrWhiteSpace(originArea) && originArea.Length <= 120 &&
        !string.IsNullOrWhiteSpace(destinationArea) && destinationArea.Length <= 120 &&
        departure > DateTimeOffset.UtcNow && expiry > DateTimeOffset.UtcNow && expiry <= departure;

    private static bool ValidLocation(LocationInput x) =>
        x.Latitude is >= -90 and <= 90 && x.Longitude is >= -180 and <= 180 &&
        !string.IsNullOrWhiteSpace(x.Address) && x.Address.Length <= 500;

    private static Point Point(LocationInput x) => new(x.Longitude, x.Latitude) { SRID = 4326 };
}

public sealed record LocationInput(double Latitude, double Longitude, string Address);
public sealed record VehicleInput(string Type, string? Model, string? Color, string? LicensePlate, int ShareableSeats);
public sealed record TripInput(LocationInput Origin, LocationInput Destination, string OriginArea,
    string DestinationArea, DateTimeOffset DepartureTime, DateTimeOffset ExpiresAt,
    Guid? VehicleId, int AvailableSeats, int PassengerCount, decimal? ContributionAmount);
public sealed record RequestInput(LocationInput Origin, LocationInput Destination, string OriginArea,
    string DestinationArea, DateTimeOffset DesiredTime, DateTimeOffset ExpiresAt,
    int PassengerCount, string? PreferredVehicleType, decimal? ProposedContribution);
