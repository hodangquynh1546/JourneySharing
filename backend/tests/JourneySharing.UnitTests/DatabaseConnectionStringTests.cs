using JourneySharing.Infrastructure;
using Npgsql;

namespace JourneySharing.UnitTests;

public sealed class DatabaseConnectionStringTests
{
    [Fact]
    public void Converts_render_postgres_url_without_losing_encoded_password()
    {
        var text = DatabaseConnectionString.ForNpgsql(
            "postgresql://member:pa%3Ass%3Bword@database.internal:5432/journeysharing");
        var parsed = new NpgsqlConnectionStringBuilder(text);
        Assert.Equal("database.internal", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("journeysharing", parsed.Database);
        Assert.Equal("member", parsed.Username);
        Assert.Equal("pa:ss;word", parsed.Password);
    }

    [Fact]
    public void Keeps_existing_npgsql_connection_string()
    {
        const string text = "Host=localhost;Database=journeysharing;Username=member";
        Assert.Equal(text, DatabaseConnectionString.ForNpgsql(text));
    }

    [Fact]
    public void Rejects_url_query_instead_of_silently_ignoring_ssl_settings()
    {
        Assert.Throws<ArgumentException>(() => DatabaseConnectionString.ForNpgsql(
            "postgresql://member:password@database.internal/journeysharing?sslmode=require"));
    }
}
