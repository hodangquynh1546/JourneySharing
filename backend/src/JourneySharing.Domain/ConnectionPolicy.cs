namespace JourneySharing.Domain;

public static class ConnectionPolicy
{
    public static bool CanTransition(ConnectionStatus current, ConnectionStatus target, bool isSender) =>
        current == ConnectionStatus.Pending &&
        (target == ConnectionStatus.Cancelled ? isSender :
            (target is ConnectionStatus.Accepted or ConnectionStatus.Rejected) && !isSender);

    public static bool HasCapacity(int totalSeats, int occupiedSeats, int requestedSeats) =>
        totalSeats > 0 && occupiedSeats >= 0 && requestedSeats > 0 &&
        occupiedSeats <= totalSeats && requestedSeats <= totalSeats - occupiedSeats;
}
