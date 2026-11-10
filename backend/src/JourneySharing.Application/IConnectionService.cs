using JourneySharing.Domain;

namespace JourneySharing.Application;

public sealed record ConnectionIntent(Guid TripId, Guid? TravelRequestId, Guid? OtherTripId);
public sealed record ConnectionResult(int StatusCode, string? Error = null, Guid? Id = null);

public interface IConnectionService
{
    Task<ConnectionResult> CreateAsync(Guid actorId, ConnectionIntent intent, CancellationToken cancellationToken);
    Task<ConnectionResult> AcceptAsync(Guid actorId, Guid connectionId, CancellationToken cancellationToken);
    Task<ConnectionResult> RejectAsync(Guid actorId, Guid connectionId, CancellationToken cancellationToken);
    Task<ConnectionResult> CancelAsync(Guid actorId, Guid connectionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TripConnection>> ListMineAsync(Guid actorId, CancellationToken cancellationToken);
}
