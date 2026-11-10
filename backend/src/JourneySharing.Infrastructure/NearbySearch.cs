using JourneySharing.Domain;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace JourneySharing.Infrastructure;

public static class NearbySearch
{
    public static IQueryable<Trip> FilterTrips(IQueryable<Trip> query,
        Point? origin, Point? destination, double radiusMeters)
    {
        if (origin is not null)
            query = query.Where(x => EF.Functions.IsWithinDistance(x.Origin, origin, radiusMeters, true));
        if (destination is not null)
            query = query.Where(x => EF.Functions.IsWithinDistance(x.Destination, destination, radiusMeters, true));
        return query;
    }

    public static IQueryable<TravelRequest> FilterRequests(IQueryable<TravelRequest> query,
        Point? origin, Point? destination, double radiusMeters)
    {
        if (origin is not null)
            query = query.Where(x => EF.Functions.IsWithinDistance(x.Origin, origin, radiusMeters, true));
        if (destination is not null)
            query = query.Where(x => EF.Functions.IsWithinDistance(x.Destination, destination, radiusMeters, true));
        return query;
    }
}
