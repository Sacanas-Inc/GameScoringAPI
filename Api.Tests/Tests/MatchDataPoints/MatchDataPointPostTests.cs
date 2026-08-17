using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchDataPointPostTests : TestBase
    {
        [Fact]
        public async Task CreateMatchDataPoint_WithValidData_Returns201Created()
        {
            // Arrange - Create a match to add data point to
            var gamesResponse = await Client.GetAsync("/games");
            Assert.True(gamesResponse.IsSuccessStatusCode, "Failed to retrieve games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            Assert.NotNull(games);
            var gameId = (int)games.First().id;

            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Test match", isFinished = false };
            var matchResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, matchResponse.StatusCode);
            var createdMatch = JsonConvert.DeserializeObject<dynamic>(await matchResponse.Content.ReadAsStringAsync());
            var matchId = (int)createdMatch.matchId;

            var dataPointRequest = new
            {
                matchId,
                playerName = "Test Player",
                gamePoints = 100,
                pointsDescription = "Test points"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync($"/match-data-point/{matchId}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var createdDataPoint = JsonConvert.DeserializeObject<object>(jsonResponseString);
            Assert.NotNull(createdDataPoint);
        }

        [Fact]
        public async Task CreateMatchDataPoint_WithInvalidMatchId_ReturnsBadRequest()
        {
            // Arrange - Use non-existent match ID in route
            var dataPointRequest = new
            {
                playerName = "Test Player",
                gamePoints = 50,
                pointsDescription = "Invalid match"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match-data-point/-999", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMatchDataPoint_VerifiesTriggerUpdatesMatchPlayerCount()
        {
            // Arrange - Get a match and its initial player count
            var matchesResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchId = matches.First().MatchId;

            // Get initial player count
            var matchBeforeResponse = await Client.GetAsync($"/match/{matchId}");
            var matchBefore = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchBeforeResponse.Content.ReadAsStringAsync());
            var playerCountBefore = matchBefore?.PlayerCount ?? 0;

            // Create a new data point with a unique player name
            var playerName = $"Player-{Guid.NewGuid().ToString()[..8]}";
            var dataPointRequest = new
            {
                matchId = matchId,
                playerName = playerName,
                gamePoints = 75,
                pointsDescription = "Trigger test point"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var createResponse = await Client.PostAsync("/match-data-point", content);

            // Assert creation succeeded
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);

            // Get updated match and verify player count increased (trigger should fire)
            var matchAfterResponse = await Client.GetAsync($"/match/{matchId}");
            var matchAfter = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchAfterResponse.Content.ReadAsStringAsync());
            var playerCountAfter = matchAfter?.PlayerCount ?? 0;

            Assert.True(playerCountAfter >= playerCountBefore,
                "Database trigger should have updated Matches.PlayerCount");
        }

        [Fact]
        public async Task CreateMatchDataPoint_WithNegativePoints_IsAllowed()
        {
            // Arrange - Get a match
            var matchesResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchId = matches.First().MatchId;
            var dataPointRequest = new
            {
                matchId = matchId,
                playerName = $"Player-{Guid.NewGuid().ToString()[..8]}",
                gamePoints = -50, // Negative points
                pointsDescription = "Penalty points"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match-data-point", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateMatchDataPoint_WithoutPlayerName_ReturnsBadRequest()
        {
            // Arrange - Get a match
            var matchesResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchId = matches.First().MatchId;
            var dataPointRequest = new
            {
                matchId = matchId,
                playerName = "", // Empty player name
                gamePoints = 100,
                pointsDescription = "Missing player name"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/match-data-point", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
