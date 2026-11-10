using System.Data;
using JourneySharing.Application;
using JourneySharing.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JourneySharing.Infrastructure;

public sealed class ConnectionService(JourneyDbContext db) : IConnectionService
{
    public async Task<ConnectionResult> CreateAsync(Guid actorId, ConnectionIntent intent, CancellationToken ct)
    {
        if ((intent.TravelRequestId is null) == (intent.OtherTripId is null))
            return new(400, "Choose exactly one counterpart.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var driver = await db.Trips.AsNoTracking().SingleOrDefaultAsync(x => x.Id == intent.TripId, ct);
        if (driver is null || driver.VehicleId is null) return new(404, "Driver journey not found.");
        if (driver.Status != TripStatus.Open || driver.ExpiresAt <= DateTimeOffset.UtcNow)
            return new(409, "Driver journey is closed.");

        Guid counterpartOwner;
        int passengerCount;
        DateTimeOffset counterpartTime;
        if (intent.TravelRequestId is Guid requestId)
        {
            var request = await db.TravelRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId, ct);
            if (request is null) return new(404, "Travel request not found.");
            if (request.Status != RequestStatus.Open || request.ExpiresAt <= DateTimeOffset.UtcNow)
                return new(409, "Travel request is closed.");
            counterpartOwner = request.UserId;
            passengerCount = request.PassengerCount;
            counterpartTime = request.DesiredTime;
        }
        else
        {
            var passengerTrip = await db.Trips.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == intent.OtherTripId, ct);
            if (passengerTrip is null || passengerTrip.VehicleId is not null)
                return new(404, "Passenger journey not found.");
            if (passengerTrip.Status != TripStatus.Open || passengerTrip.ExpiresAt <= DateTimeOffset.UtcNow)
                return new(409, "Passenger journey is closed.");
            counterpartOwner = passengerTrip.UserId;
            passengerCount = passengerTrip.PassengerCount;
            counterpartTime = passengerTrip.DepartureTime;
        }
        if (driver.UserId == counterpartOwner ||
            (actorId != driver.UserId && actorId != counterpartOwner))
            return new(403, "Only a participant may connect distinct users.");
        if (Math.Abs((driver.DepartureTime - counterpartTime).TotalMinutes) > 90)
            return new(422, "Departure times differ by more than 90 minutes.");
        if (passengerCount > driver.AvailableSeats)
            return new(409, "Passenger count exceeds vehicle capacity.");
        var connection = new TripConnection
        {
            TripId = driver.Id, TravelRequestId = intent.TravelRequestId,
            OtherTripId = intent.OtherTripId, SenderId = actorId,
            ReservedSeats = passengerCount
        };
        db.Connections.Add(connection);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(201, Id: connection.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return new(409, "A connection between these posts already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
        {
            return new(409, "Concurrent update. Retry the request.");
        }
        catch (PostgresException ex) when (ex.SqlState == "40001")
        {
            return new(409, "Concurrent update. Retry the request.");
        }
    }

    public Task<ConnectionResult> AcceptAsync(Guid actorId, Guid connectionId, CancellationToken ct) =>
        TransitionAsync(actorId, connectionId, ConnectionStatus.Accepted, ct);

    public Task<ConnectionResult> RejectAsync(Guid actorId, Guid connectionId, CancellationToken ct) =>
        TransitionAsync(actorId, connectionId, ConnectionStatus.Rejected, ct);

    public Task<ConnectionResult> CancelAsync(Guid actorId, Guid connectionId, CancellationToken ct) =>
        TransitionAsync(actorId, connectionId, ConnectionStatus.Cancelled, ct);

    private async Task<ConnectionResult> TransitionAsync(Guid actorId, Guid id, ConnectionStatus target, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var connection = await db.Connections.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (connection is null) return new(404, "Connection not found.");
            var driver = await db.Trips.AsNoTracking().SingleAsync(x => x.Id == connection.TripId, ct);
            Guid counterpartOwner = connection.TravelRequestId is Guid requestId
                ? await db.TravelRequests.Where(x => x.Id == requestId).Select(x => x.UserId).SingleAsync(ct)
                : await db.Trips.Where(x => x.Id == connection.OtherTripId).Select(x => x.UserId).SingleAsync(ct);
            if (actorId != driver.UserId && actorId != counterpartOwner) return new(403);
            if (connection.Status != ConnectionStatus.Pending)
                return new(409, "Only a pending connection can be changed.");
            if (!ConnectionPolicy.CanTransition(connection.Status, target, actorId == connection.SenderId))
                return new(403);

            if (target == ConnectionStatus.Accepted)
            {
                if (driver.Status != TripStatus.Open || driver.ExpiresAt <= DateTimeOffset.UtcNow)
                    return new(409, "Driver journey is closed.");
                var counterpartOpen = connection.TravelRequestId is Guid requestIdForCheck
                    ? await db.TravelRequests.AnyAsync(x => x.Id == requestIdForCheck &&
                        x.Status == RequestStatus.Open && x.ExpiresAt > DateTimeOffset.UtcNow, ct)
                    : await db.Trips.AnyAsync(x => x.Id == connection.OtherTripId &&
                        x.Status == TripStatus.Open && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
                if (!counterpartOpen) return new(409, "Counterpart post is closed.");
                var activeSeats = await db.Connections
                    .Where(x => x.TripId == driver.Id &&
                        (x.Status == ConnectionStatus.Accepted || x.Status == ConnectionStatus.InProgress))
                    .SumAsync(x => (int?)x.ReservedSeats, ct) ?? 0;
                if (!ConnectionPolicy.HasCapacity(driver.AvailableSeats, activeSeats, connection.ReservedSeats))
                    return new(409, "No seats remain.");
            }
            connection.Status = target;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(200, Id: connection.Id);
        }
        catch (PostgresException ex) when (ex.SqlState == "40001")
        {
            return new(409, "Concurrent update. Retry the request.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
        {
            return new(409, "Concurrent update. Retry the request.");
        }
    }

    public async Task<IReadOnlyList<TripConnection>> ListMineAsync(Guid actorId, CancellationToken ct) =>
        await db.Connections.AsNoTracking().Where(c =>
            db.Trips.Any(t => t.Id == c.TripId && t.UserId == actorId) ||
            (c.TravelRequestId != null && db.TravelRequests.Any(r => r.Id == c.TravelRequestId && r.UserId == actorId)) ||
            (c.OtherTripId != null && db.Trips.Any(t => t.Id == c.OtherTripId && t.UserId == actorId)))
            .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct);
}
