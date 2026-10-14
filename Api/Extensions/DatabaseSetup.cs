using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameScoringAPI.Extensions;

public static class DatabaseSetup
{
    private const string LoggerCategory = "GameScoringAPI.Database";

    /// <summary>
    /// Configure DbContext using DATABASE_URL env var (preferred) or DefaultConnection from config.
    /// Throws if no connection available.
    ///
    /// Extend the WebApplicationBuilder using the THIS keyword, adding the methods to the builder without modifying the original class.
    /// </summary>
    public static void ConfigureDatabaseServices(this WebApplicationBuilder builder)
    {
        var (connectionString, info) = ResolveConnectionString(builder.Configuration);

        // Expose the safe connection description and initialization state for startup logging and health checks.
        builder.Services.AddSingleton(info);
        builder.Services.AddSingleton<DatabaseInitializationState>();

        // Register DbContext
        builder.Services.AddDbContext<GameDBContext>(options => options.UseNpgsql(connectionString));
    }

    /// <summary>
    /// Resolves the Postgres connection string (preferring ConnectionStrings:DatabaseUrl, then
    /// DefaultConnection) and returns it together with a password-free description for logging.
    /// Throw if neither is configured.
    /// </summary>
    private static (string connectionString, DatabaseConnectionInfo info) ResolveConnectionString(IConfiguration configuration)
    {
        var envDatabaseUrl = configuration.GetConnectionString("DatabaseUrl");
        var configConn = configuration.GetConnectionString("DefaultConnection");

        // Prefer DATABASE_URL, else appsettings DefaultConnection
        if (!string.IsNullOrWhiteSpace(envDatabaseUrl))
        {
            try
            {
                var uri = new Uri(envDatabaseUrl);
                var userInfo = uri.UserInfo.Split(':', 2);
                var user = userInfo.Length > 0 ? userInfo[0] : string.Empty;
                var pass = userInfo.Length > 1 ? userInfo[1] : string.Empty;

                var csBuilder = new Npgsql.NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Username = user,
                    Password = pass,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    SslMode = Npgsql.SslMode.Require
                };

                var connectionString = csBuilder.ToString();
                var info = DatabaseConnectionInfo.FromConnectionString("DatabaseUrl", connectionString);
                return (connectionString, info);
            }
            catch (Exception ex)
            {
                // No logger is available yet during the builder phase; carry the warning into startup logging.
                var warning = $"Failed to parse ConnectionStrings:DatabaseUrl (expected a postgresql:// URI): {ex.Message}";

                if (!string.IsNullOrWhiteSpace(configConn))
                {
                    return (configConn, DatabaseConnectionInfo.FromConnectionString("DefaultConnection", configConn, warning));
                }

                throw new InvalidOperationException($"{warning} No fallback DefaultConnection is configured.", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(configConn))
        {
            return (configConn, DatabaseConnectionInfo.FromConnectionString("DefaultConnection", configConn));
        }

        throw new InvalidOperationException(
            "No Postgres connection configured. Set the App Setting 'ConnectionStrings__DatabaseUrl' or DefaultConnection in configuration.");
    }

    /// <summary>
    /// Apply migrations (which now include the PL/pgSQL functions and triggers) for Postgres.
    /// Safe to call multiple times.
    ///
    /// On migration failure the app logs a critical error and, unless Database:FailFastOnStartup is true,
    /// continues in degraded mode so the health endpoint can report the failure instead of a bare 500.30.
    /// </summary>
    public static void InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory);
        var dbContext = services.GetRequiredService<GameDBContext>();
        var state = services.GetRequiredService<DatabaseInitializationState>();
        var info = services.GetRequiredService<DatabaseConnectionInfo>();
        var configuration = services.GetRequiredService<IConfiguration>();

        logger.LogInformation("Database connection resolved. {DatabaseTarget}", info);

        if (info.ResolutionWarning is not null)
        {
            logger.LogWarning("{Warning}", info.ResolutionWarning);
        }

        if (info.Source == "DefaultConnection" && !app.Environment.IsDevelopment())
        {
            logger.LogWarning(
                "ConnectionStrings:DatabaseUrl is not set; falling back to DefaultConnection for environment {Environment}. " +
                "In Azure set the App Setting 'ConnectionStrings__DatabaseUrl'.",
                app.Environment.EnvironmentName);
        }

        var failFast = configuration.GetValue("Database:FailFastOnStartup", false);

        try
        {
            logger.LogInformation("Applying database migrations to {Host}/{Database}...", info.Host, info.Database);
            dbContext.Database.Migrate();

            var applied = dbContext.Database.GetAppliedMigrations().ToList();
            logger.LogInformation(
                "Database migrations applied. Count={Count}, Migrations={Migrations}",
                applied.Count,
                string.Join(", ", applied));
        }
        catch (Exception ex)
        {
            state.MarkFailed(ex.Message);
            logger.LogCritical(ex, "Database migration failed for Host={Host}, Database={Database}.", info.Host, info.Database);

            if (failFast)
            {
                throw;
            }

            logger.LogError(
                "Continuing in degraded mode: database endpoints will fail and GET /health will report Unhealthy. " +
                "Set 'Database:FailFastOnStartup' to true to crash on startup instead.");
            return;
        }

        state.MarkSucceeded();
    }
}
