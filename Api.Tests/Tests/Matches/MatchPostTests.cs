using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchPostTests : TestBase
    {
        [Fact]
        public async Task CreateMatch_WithValidData_Returns201Created()
        {
            // Arrange - Get a valid game ID from seeded data
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());

            if (games == null || games.Count == 0)
            {
                Assert.True(false, "No games in database to test match creation");
                return;
            }

            var gameId = games.First().Id;
            var matchRequest = new
            {
                gameId = gameId,
                matchDate = DateTime.UtcNow,
                notes = "Test match",
                isFinished = false
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var createdMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(jsonResponseString);
            Assert.NotNull(createdMatch);
            Assert.Equal(gameId, createdMatch.GameId);
        }

        [Fact]
        public async Task CreateMatch_WithInvalidGameId_ReturnsBadRequest()
        {
            // Arrange - Use non-existent game ID
            var matchRequest = new
            {
                gameId = -999,
                matchDate = DateTime.UtcNow,
                notes = "Test match with invalid game",
                isFinished = false
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var errorResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);
            Assert.NotNull(errorResponse["errors"]);
        }

        [Fact]
        public async Task CreateMatch_WithNoNotes_Returns201Created()
        {
            // Arrange - Get a valid game ID from seeded data
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());

            if (games == null || games.Count == 0)
                return;

            var gameId = games.First().Id;
            var matchRequest = new
            {
                gameId = gameId,
                matchDate = DateTime.UtcNow,
                notes = (string)null,
                isFinished = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var createdMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(jsonResponseString);
            Assert.NotNull(createdMatch);
            Assert.True(createdMatch.isFinished);
        }

        [Fact]
        public async Task CreateMatch_VerifysTriggerUpdatesGameMatchCount()
        {
            // Arrange - Get a valid game ID and its initial match count
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());

            if (games == null || games.Count == 0)
                return;

            var gameId = games.First().Id;

            // Get initial match count
            var gameBeforeResponse = await Client.GetAsync($"/game/{gameId}");
            var gameBefore = JsonConvert.DeserializeObject<GameDto>(
                await gameBeforeResponse.Content.ReadAsStringAsync());
            var matchCountBefore = gameBefore?.MatchesCount ?? 0;

            // Create a new match
            var matchRequest = new
            {
                gameId = gameId,
                matchDate = DateTime.UtcNow,
                notes = "Trigger test match",
                isFinished = false
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var createResponse = await Client.PostAsync("/match", content);

            // Assert creation succeeded
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);

            // Get updated game and verify match count increased (trigger should fire)
            var gameAfterResponse = await Client.GetAsync($"/game/{gameId}");
            var gameAfter = JsonConvert.DeserializeObject<GameDto>(
                await gameAfterResponse.Content.ReadAsStringAsync());
            var matchCountAfter = gameAfter?.MatchesCount ?? 0;

            Assert.True(matchCountAfter > matchCountBefore, 
                "Database trigger should have incremented Games.MatchesCount");
        }
    }
}
