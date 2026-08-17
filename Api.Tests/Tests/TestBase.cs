using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests
{
    public class TestBase
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected readonly HttpClient Client;
        protected readonly GameDBContext DbContext;
        private static readonly object _dbLock = new object();
        private readonly string _testDatabaseName;

        public TestBase()
        {
            // Create unique database name per test to avoid conflicts
            _testDatabaseName = $"gamescoringapi_test_{Guid.NewGuid().ToString("N")[..8]}";

            lock (_dbLock)
            {
                // Create a NEW factory for each test
                var factory = new WebApplicationFactory<Program>();
                
                Factory = factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the existing DbContext registration (if any)
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<GameDBContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Use unique database name for this test
                        var testConnectionString = $"Host=localhost;Port=5433;Username=testuser;Password=testpassword;Database={_testDatabaseName}";
                        services.AddDbContext<GameDBContext>(options =>
                        {
                            options.UseNpgsql(testConnectionString);
                        });
                    });
                });

                // Create the client (app startup runs migrations to create fresh schema)
                Client = Factory.CreateClient();

                // Wait for database to be ready after migrations
                WaitForDatabaseReady();

                // Get DbContext and seed test data
                using var scope = Factory.Services.CreateScope();
                DbContext = scope.ServiceProvider.GetRequiredService<GameDBContext>();
                SeedGamesData(DbContext);
            }
        }

        private void WaitForDatabaseReady()
        {
            var maxRetries = 10;
            var retryCount = 0;
            var connectionString = $"Host=localhost;Port=5433;Username=testuser;Password=testpassword;Database={_testDatabaseName}";

            while (retryCount < maxRetries)
            {
                try
                {
                    var options = new DbContextOptionsBuilder<GameDBContext>()
                        .UseNpgsql(connectionString)
                        .Options;

                    using (var context = new GameDBContext(options))
                    {
                        // Simple query to verify database is ready
                        context.Database.ExecuteSqlRaw("SELECT 1");
                    }
                    return; // Database is ready
                }
                catch
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        private JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        public List<T> GetDto<T>(string name)
        {
            var jsonDto = File.ReadAllText($"./Data/{name}.json");
            return JsonSerializer.Deserialize<List<T>>(jsonDto, GetOptions());
        }

        protected void SeedGamesData(GameDBContext context)
        {
            // This will load the ./Data/GamesGetTests/GamesData.Json
            var dto = GetDto<GameDto>("GamesData");
            foreach (GameDto game in dto)
            {
                context.Games.Add(new Game
                {
                    GameName = game.GameName,
                    GameDescription = game.GameDescription,
                    MinPlayers = game.MinPlayers,
                    MaxPlayers = game.MaxPlayers,
                    AverageDuration = game.AverageDuration,
                    MatchesCount = game.MatchesCount
                });
            }
            context.SaveChanges();
        }
    }
}