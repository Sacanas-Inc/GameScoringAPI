using Microsoft.EntityFrameworkCore;

namespace GameScoringAPI.Extensions;

public static class DatabaseSetup
{
    /// <summary>
    /// Configure DbContext using DATABASE_URL env var (preferred) or DefaultConnection from config.
    /// Throws if no connection available.
    /// 
    /// Extend the WebApplicationBuilder using the THIS keyword, adding the methods to the builder without modifying the original class.
    /// </summary>
    public static void ConfigureDatabaseServices(this WebApplicationBuilder builder)
    {
        // TODO: .env file? It doesnt seem right to use appsettings
        var envDatabaseUrl = builder.Configuration.GetConnectionString("DatabaseUrl");
        var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
        string? connectionString = null;

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
                    SslMode = Npgsql.SslMode.Require,
                    TrustServerCertificate = true
                };

                connectionString = csBuilder.ToString();
                Console.WriteLine($"Using DATABASE_URL Postgres. Host: {csBuilder.Host}, DB: {csBuilder.Database}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse DATABASE_URL: {ex.Message}");
                connectionString = null;
            }
        } else {
            Console.WriteLine($"DATABASE_URL not set. Checking DefaultConnection in configuration.");
        }

        if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(configConn))
        {
            connectionString = configConn;
            Console.WriteLine($"Using DefaultConnection from configuration.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No Postgres connection configured. Set DATABASE_URL env var or DefaultConnection in configuration.");
        }

        // Register DbContext
        builder.Services.AddDbContext<GameDBContext>(options => options.UseNpgsql(connectionString));
    }

    /// <summary>
    /// Apply migrations and create PL/pgSQL functions + triggers for Postgres.
    /// </summary>
    public static void InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameDBContext>();

        // Apply migrations for relational provider (Postgres)
        dbContext.Database.Migrate();

        if (dbContext.Database.IsRelational())
        {
            // Create PL/pgSQL functions (must exist before triggers)
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesTriggerInsertFunction);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesTriggerDeleteFunction);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesDataPointTriggerInsertFunction);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesDataPointTriggerDeleteFunction);

            // Create triggers that call the functions
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesTriggerInsert);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesTriggerDelete);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesDataPointTriggerInsert);
            dbContext.Database.ExecuteSqlRaw(SqlTriggers.MatchesDataPointTriggerDelete);
        }
    }
}
