using NetTopologySuite.Geometries;

namespace JourneySharing.Domain;

public enum TripStatus { Open, Cancelled, InProgress, Completed, Expired }
public enum RequestStatus { Open, Cancelled, Matched, Completed, Expired }
public enum ConnectionStatus { Pending, Accepted, Rejected, Cancelled, InProgress, Completed, Disputed }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? LicensePlate { get; set; }
    public int ShareableSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    // A planned journey can be posted by someone who needs a ride.
    public Guid? VehicleId { get; set; }
    public required Point Origin { get; set; }
    public required Point Destination { get; set; }
    public required string OriginAddress { get; set; }
    public required string DestinationAddress { get; set; }
    public required string OriginArea { get; set; }
    public required string DestinationArea { get; set; }
    public LineString? Route { get; set; }
    public DateTimeOffset DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
    public int PassengerCount { get; set; } = 1;
    public decimal? ContributionAmount { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Open;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TravelRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required Point Origin { get; set; }
    public required Point Destination { get; set; }
    public required string OriginAddress { get; set; }
    public required string DestinationAddress { get; set; }
    public required string OriginArea { get; set; }
    public required string DestinationArea { get; set; }
    public DateTimeOffset DesiredTime { get; set; }
    public int PassengerCount { get; set; }
    public string? PreferredVehicleType { get; set; }
    public decimal? ProposedContribution { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TripConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    // Exactly one counterpart is set: a travel request or another journey.
    public Guid? TravelRequestId { get; set; }
    public Guid? OtherTripId { get; set; }
    public Guid SenderId { get; set; }
    public int ReservedSeats { get; set; }
    public decimal? AgreedContribution { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConnectionMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConnectionId { get; set; }
    public Guid SenderId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
