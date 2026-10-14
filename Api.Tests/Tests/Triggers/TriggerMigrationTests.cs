using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests
{
    /// <summary>
    /// Automated equivalent of running <c>dotnet ef database update</c> against a fresh
    /// database: applies the migrations directly (no <see cref="TestBase"/>, no
    /// <c>WebApplicationFactory</c>, no <c>InitializeDatabase</c>) and asserts that the
    /// migration itself creates the denormalized-count functions/triggers and that they
    /// maintain the counters. Guards the move of trigger creation out of startup.
    /// </summary>
    public class TriggerMigrationTests
    {
        private const string ConnectionTemplate =
            "Host=localhost;Port=5433;Username=testuser;Password=testpassword;Database={0}";

        private static DbContextOptions<GameDBContext> BuildOptions(string databaseName) =>
            new DbContextOptionsBuilder<GameDBContext>()
                .UseNpgsql(string.Format(CultureInfo.InvariantCulture, ConnectionTemplate, databaseName))
                .Options;

        [Fact]
        public async Task Migrate_CreatesTriggersThatMaintainDenormalizedCounters()
        {
            var databaseName = $"gamescoringapi_test_{Guid.NewGuid().ToString("N")[..8]}";

            await using var context = new GameDBContext(BuildOptions(databaseName));

            // Apply migrations only - the application startup path is deliberately not used.
            await context.Database.MigrateAsync();

            // 1) The four triggers must exist.
            var triggerNames = await QueryStringsAsync(context,
                "SELECT tgname FROM pg_trigger WHERE NOT tgisinternal;");
            Assert.Contains("trg_update_game_match_count_insert", triggerNames);
            Assert.Contains("trg_update_game_match_count_delete", triggerNames);
            Assert.Contains("trg_update_match_player_count_insert", triggerNames);
            Assert.Contains("trg_update_match_player_count_delete", triggerNames);

            // 2) The four trigger functions must exist.
            var functionNames = await QueryStringsAsync(context,
                "SELECT proname FROM pg_proc WHERE proname LIKE 'update_%_count_on_%';");
            Assert.Contains("update_game_match_count_on_insert", functionNames);
            Assert.Contains("update_game_match_count_on_delete", functionNames);
            Assert.Contains("update_match_player_count_on_insert", functionNames);
            Assert.Contains("update_match_player_count_on_delete", functionNames);

            // 3) Games.MatchesCount updates on Match insert/delete.
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Games\" (\"GameName\", \"MinPlayers\", \"MaxPlayers\", \"AverageDuration\", \"MatchesCount\") VALUES ('TriggerMigrationTest', 2, 2, 30, 0);");
            var gameId = await QueryScalarAsync(context,
                "SELECT \"Id\" FROM \"Games\" WHERE \"GameName\" = 'TriggerMigrationTest';");

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Matches\" (\"GameId\", \"MatchDate\", \"isFinished\", \"PlayerCount\") VALUES ({0}, NOW(), false, 0);",
                gameId);
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Matches\" (\"GameId\", \"MatchDate\", \"isFinished\", \"PlayerCount\") VALUES ({0}, NOW(), false, 0);",
                gameId);
            Assert.Equal(2, await QueryScalarAsync(context,
                $"SELECT \"MatchesCount\" FROM \"Games\" WHERE \"Id\" = {gameId};"));

            var matchId = await QueryScalarAsync(context,
                $"SELECT \"Id\" FROM \"Matches\" WHERE \"GameId\" = {gameId} ORDER BY \"Id\" LIMIT 1;");
            await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Matches\" WHERE \"Id\" = {0};", matchId);
            Assert.Equal(1, await QueryScalarAsync(context,
                $"SELECT \"MatchesCount\" FROM \"Games\" WHERE \"Id\" = {gameId};"));

            // 4) Matches.PlayerCount updates on MatchDataPoint insert/delete.
            var remainingMatchId = await QueryScalarAsync(context,
                $"SELECT \"Id\" FROM \"Matches\" WHERE \"GameId\" = {gameId} LIMIT 1;");

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"MatchDataPoints\" (\"MatchId\", \"PlayerName\", \"GamePoints\", \"PointsDescription\", \"CreatedDate\") VALUES ({0}, 'Alice', 10, 'Trigger test', NOW());",
                remainingMatchId);
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"MatchDataPoints\" (\"MatchId\", \"PlayerName\", \"GamePoints\", \"PointsDescription\", \"CreatedDate\") VALUES ({0}, 'Bob', 5, 'Trigger test', NOW());",
                remainingMatchId);
            Assert.Equal(2, await QueryScalarAsync(context,
                $"SELECT \"PlayerCount\" FROM \"Matches\" WHERE \"Id\" = {remainingMatchId};"));

            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"MatchDataPoints\" WHERE \"MatchId\" = {0};", remainingMatchId);
            Assert.Equal(0, await QueryScalarAsync(context,
                $"SELECT \"PlayerCount\" FROM \"Matches\" WHERE \"Id\" = {remainingMatchId};"));
        }

        private static async Task<List<string>> QueryStringsAsync(GameDBContext context, string sql)
        {
            var results = new List<string>();
            var connection = context.Database.GetDbConnection();
            await context.Database.OpenConnectionAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetString(0));
                }
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }

            return results;
        }

        private static async Task<int> QueryScalarAsync(GameDBContext context, string sql)
        {
            var connection = context.Database.GetDbConnection();
            await context.Database.OpenConnectionAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
