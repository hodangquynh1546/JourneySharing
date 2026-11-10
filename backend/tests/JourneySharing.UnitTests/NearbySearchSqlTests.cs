using JourneySharing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace JourneySharing.UnitTests;

public sealed class NearbySearchSqlTests
{
    private static JourneyDbContext Context()
    {
        var options = new DbContextOptionsBuilder<JourneyDbContext>()
            .UseNpgsql("Host=localhost;Database=journey_test;Username=local",
                npgsql => npgsql.UseNetTopologySuite()).Options;
        return new JourneyDbContext(options);
    }

    [Fact]
    public void Trip_filter_translates_both_points_to_postgis_distance_checks()
    {
        using var db = Context();
        var origin = new Point(107.59, 16.46) { SRID = 4326 };
        var destination = new Point(108.21, 16.06) { SRID = 4326 };
        var sql = NearbySearch.FilterTrips(db.Trips, origin, destination, 10000).ToQueryString();
        Assert.Equal(2, Count(sql, "ST_DWithin"));
        Assert.Contains("\"Origin\"", sql);
        Assert.Contains("\"Destination\"", sql);
    }

    [Fact]
    public void Request_filter_translates_both_points_to_postgis_distance_checks()
    {
        using var db = Context();
        var origin = new Point(107.59, 16.46) { SRID = 4326 };
        var destination = new Point(108.21, 16.06) { SRID = 4326 };
        var sql = NearbySearch.FilterRequests(db.TravelRequests, origin, destination, 10000).ToQueryString();
        Assert.Equal(2, Count(sql, "ST_DWithin"));
    }

    private static int Count(string text, string fragment) =>
        text.Split(fragment, StringSplitOptions.None).Length - 1;
}
