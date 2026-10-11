using Npgsql;

namespace GameScoringAPI.Extensions;

/// <summary>
/// Safe, loggable description of the resolved database connection.
/// Intentionally never carries the password, so it can be written to logs.
/// </summary>
public sealed record DatabaseConnectionInfo(
    string Source,
    string Host,
    int Port,
    string Database,
    string? Username,
    string? ResolutionWarning = null)
{
    /// <summary>
    /// Builds the safe description from a Postgres connection string, extracting only the
    /// non-secret parts (host, port, database, username). The password is discarded.
    /// </summary>
    public static DatabaseConnectionInfo FromConnectionString(string source, string connectionString, string? resolutionWarning = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        return new DatabaseConnectionInfo(
            source,
            string.IsNullOrWhiteSpace(builder.Host) ? "(unknown)" : builder.Host,
            builder.Port,
            string.IsNullOrWhiteSpace(builder.Database) ? "(unknown)" : builder.Database,
            builder.Username,
            resolutionWarning);
    }

    public override string ToString() =>
        $"Source={Source}, Host={Host}, Port={Port}, Database={Database}, User={Username ?? "(none)"}";
}
