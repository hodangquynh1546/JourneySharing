using JourneySharing.Domain;
using Microsoft.EntityFrameworkCore;

namespace JourneySharing.Infrastructure;

public sealed class JourneyDbContext(DbContextOptions<JourneyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TravelRequest> TravelRequests => Set<TravelRequest>();
    public DbSet<TripConnection> Connections => Set<TripConnection>();
    public DbSet<ConnectionMessage> Messages => Set<ConnectionMessage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("postgis");
        model.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.FullName).HasMaxLength(160);
        });
        model.Entity<Vehicle>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Vehicle_Seats", "\"ShareableSeats\" > 0"));
        });
        model.Entity<Trip>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Origin).HasColumnType("geography(Point,4326)");
            e.Property(x => x.Destination).HasColumnType("geography(Point,4326)");
            e.Property(x => x.Route).HasColumnType("geometry(LineString,4326)");
            e.Property(x => x.ContributionAmount).HasPrecision(12, 2);
            e.Property(x => x.OriginArea).HasMaxLength(120);
            e.Property(x => x.DestinationArea).HasMaxLength(120);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => new { x.Status, x.DepartureTime });
            e.HasIndex(x => x.Origin).HasMethod("GIST");
            e.HasIndex(x => x.Destination).HasMethod("GIST");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Trip_Seats", "(\"VehicleId\" IS NOT NULL AND \"AvailableSeats\" > 0) OR (\"VehicleId\" IS NULL AND \"AvailableSeats\" = 0)");
                t.HasCheckConstraint("CK_Trip_Passengers", "\"PassengerCount\" > 0");
                t.HasCheckConstraint("CK_Trip_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                t.HasCheckConstraint("CK_Trip_Contribution", "\"ContributionAmount\" IS NULL OR \"ContributionAmount\" >= 0");
            });
        });
        model.Entity<TravelRequest>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Origin).HasColumnType("geography(Point,4326)");
            e.Property(x => x.Destination).HasColumnType("geography(Point,4326)");
            e.Property(x => x.ProposedContribution).HasPrecision(12, 2);
            e.Property(x => x.OriginArea).HasMaxLength(120);
            e.Property(x => x.DestinationArea).HasMaxLength(120);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => new { x.Status, x.DesiredTime });
            e.HasIndex(x => x.Origin).HasMethod("GIST");
            e.HasIndex(x => x.Destination).HasMethod("GIST");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Request_Passengers", "\"PassengerCount\" > 0");
                t.HasCheckConstraint("CK_Request_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
                t.HasCheckConstraint("CK_Request_Contribution", "\"ProposedContribution\" IS NULL OR \"ProposedContribution\" >= 0");
            });
        });
        model.Entity<TripConnection>(e =>
        {
            e.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TravelRequest>().WithMany().HasForeignKey(x => x.TravelRequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Trip>().WithMany().HasForeignKey(x => x.OtherTripId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TripId, x.TravelRequestId }).IsUnique().HasFilter("\"TravelRequestId\" IS NOT NULL");
            e.HasIndex(x => new { x.TripId, x.OtherTripId }).IsUnique().HasFilter("\"OtherTripId\" IS NOT NULL");
            e.Property(x => x.AgreedContribution).HasPrecision(12, 2);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Connection_Seats", "\"ReservedSeats\" > 0");
                t.HasCheckConstraint("CK_Connection_Counterpart", "(\"TravelRequestId\" IS NULL) <> (\"OtherTripId\" IS NULL)");
                t.HasCheckConstraint("CK_Connection_DifferentTrip", "\"OtherTripId\" IS NULL OR \"OtherTripId\" <> \"TripId\"");
                t.HasCheckConstraint("CK_Connection_Contribution", "\"AgreedContribution\" IS NULL OR \"AgreedContribution\" >= 0");
            });
        });
        model.Entity<ConnectionMessage>(e =>
        {
            e.HasOne<TripConnection>().WithMany().HasForeignKey(x => x.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Text).HasMaxLength(1000);
            e.HasIndex(x => new { x.ConnectionId, x.CreatedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_Message_Text", "length(trim(\"Text\")) > 0"));
        });
    }
}
