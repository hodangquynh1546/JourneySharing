namespace JourneySharing.Domain;

using NetTopologySuite.Geometries;

public static class ConnectionPolicy
{
    public static bool CanTransition(ConnectionStatus current, ConnectionStatus target, bool isSender) =>
        (current == ConnectionStatus.Pending &&
            (target == ConnectionStatus.Cancelled ? isSender :
                (target is ConnectionStatus.Accepted or ConnectionStatus.Rejected) && !isSender)) ||
        (current == ConnectionStatus.Accepted && target == ConnectionStatus.Cancelled);

    public static bool HasCapacity(int totalSeats, int occupiedSeats, int requestedSeats) =>
        totalSeats > 0 && occupiedSeats >= 0 && requestedSeats > 0 &&
        occupiedSeats <= totalSeats && requestedSeats <= totalSeats - occupiedSeats;

    public static bool CanCancelBeforeDeparture(DateTimeOffset departure, DateTimeOffset now) =>
        departure > now;

    // A conservative fallback until street routing is available. Post coordinates stay private.
    public static bool HasNearbyEndpoints(Point driverOrigin, Point driverDestination,
        Point passengerOrigin, Point passengerDestination, double radiusKm = 50) =>
        WithinKm(driverOrigin, passengerOrigin, radiusKm) &&
        WithinKm(driverDestination, passengerDestination, radiusKm);

    private static bool WithinKm(Point first, Point second, double radiusKm)
    {
        const double earthRadiusKm = 6371.0088;
        var latitudeDifference = Radians(second.Y - first.Y);
        var longitudeDifference = Radians(second.X - first.X);
        var sineLatitude = Math.Sin(latitudeDifference / 2);
        var sineLongitude = Math.Sin(longitudeDifference / 2);
        var haversine = sineLatitude * sineLatitude +
            Math.Cos(Radians(first.Y)) * Math.Cos(Radians(second.Y)) * sineLongitude * sineLongitude;
        var distance = 2 * earthRadiusKm * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
        return distance <= radiusKm;
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
