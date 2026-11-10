using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JourneySharing.Infrastructure;

public sealed class JourneyDbContextFactory : IDesignTimeDbContextFactory<JourneyDbContext>
{
    public JourneyDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Database=journeysharing;Username=journeysharing;Password=local-only";
        var options = new DbContextOptionsBuilder<JourneyDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new JourneyDbContext(options);
    }
}
