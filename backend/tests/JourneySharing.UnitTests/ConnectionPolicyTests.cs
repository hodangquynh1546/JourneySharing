using JourneySharing.Domain;
using NetTopologySuite.Geometries;

namespace JourneySharing.UnitTests;

public sealed class ConnectionPolicyTests
{
    [Theory]
    [InlineData(ConnectionStatus.Pending, ConnectionStatus.Accepted, false, true)]
    [InlineData(ConnectionStatus.Pending, ConnectionStatus.Rejected, false, true)]
    [InlineData(ConnectionStatus.Pending, ConnectionStatus.Cancelled, true, true)]
    [InlineData(ConnectionStatus.Pending, ConnectionStatus.Accepted, true, false)]
    [InlineData(ConnectionStatus.Pending, ConnectionStatus.Cancelled, false, false)]
    [InlineData(ConnectionStatus.Accepted, ConnectionStatus.Rejected, false, false)]
    [InlineData(ConnectionStatus.Completed, ConnectionStatus.Cancelled, true, false)]
    public void Only_the_allowed_participant_can_change_a_pending_connection(
        ConnectionStatus current, ConnectionStatus next, bool isSender, bool expected)
    {
        Assert.Equal(expected, ConnectionPolicy.CanTransition(current, next, isSender));
    }

    [Theory]
    [InlineData(2, 1, 1, true)]
    [InlineData(2, 2, 1, false)]
    [InlineData(2, 1, 2, false)]
    [InlineData(2, 0, 0, false)]
    [InlineData(2, -1, 1, false)]
    public void Seat_allocation_never_exceeds_capacity(int total, int occupied, int requested, bool expected)
    {
        Assert.Equal(expected, ConnectionPolicy.HasCapacity(total, occupied, requested));
    }

    [Fact]
    public void Nearby_pickup_and_dropoff_can_connect()
    {
        Assert.True(ConnectionPolicy.HasNearbyEndpoints(
            new Point(107.59, 16.46), new Point(108.21, 16.06),
            new Point(107.60, 16.47), new Point(108.22, 16.07)));
    }

    [Fact]
    public void Reverse_direction_cannot_connect()
    {
        Assert.False(ConnectionPolicy.HasNearbyEndpoints(
            new Point(107.59, 16.46), new Point(108.21, 16.06),
            new Point(108.21, 16.06), new Point(107.59, 16.46)));
    }

    [Fact]
    public void Distant_pickup_cannot_connect()
    {
        Assert.False(ConnectionPolicy.HasNearbyEndpoints(
            new Point(107.59, 16.46), new Point(108.21, 16.06),
            new Point(106.66, 10.82), new Point(108.21, 16.06)));
    }
}
