using Npgsql;

namespace JourneySharing.Infrastructure;

public static class DatabaseConnectionString
{
    public static string ForNpgsql(string value)
    {
        if (!value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) || string.IsNullOrWhiteSpace(uri.UserInfo) ||
            string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("Invalid PostgreSQL URL.", nameof(value));

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 || credentials[0].Length == 0)
            throw new ArgumentException("PostgreSQL URL must include a user and password.", nameof(value));

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1])
        }.ConnectionString;
    }
}
