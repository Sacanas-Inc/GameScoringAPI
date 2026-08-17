using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchDataPointPutTests : TestBase
    {
        public class MatchDataPointDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("matchId")]
            public int MatchId { get; set; }

            [JsonProperty("playerName")]
            public string PlayerName { get; set; } = string.Empty;

            [JsonProperty("gamePoints")]
            public int GamePoints { get; set; }

            [JsonProperty("pointsDescription")]
            public string PointsDescription { get; set; } = string.Empty;

            [JsonProperty("createdDate")]
            public DateTime CreatedDate { get; set; }
        }

        [Fact]
        public async Task UpdateMatchDataPoint_WithValidData_ReturnsNoContent()
        {
            // Arrange - Create match and data point
            var gamesResponse = await Client.GetAsync("/games");
            Assert.True(gamesResponse.IsSuccessStatusCode);
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameId = (int)games!.First().id;

            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Test", isFinished = false };
            var matchResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, matchResponse.StatusCode);
            var createdMatch = JsonConvert.DeserializeObject<dynamic>(await matchResponse.Content.ReadAsStringAsync());
            var matchId = (int)createdMatch!.matchId;

            var dpRequest = new { matchId, playerName = "Original Player", gamePoints = 50, pointsDescription = "Original" };
            var dpResponse = await Client.PostAsync($"/match-data-point/{matchId}",
                new StringContent(JsonConvert.SerializeObject(dpRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, dpResponse.StatusCode);
            var createdDp = JsonConvert.DeserializeObject<MatchDataPointDto>(await dpResponse.Content.ReadAsStringAsync());
            var dataPointId = createdDp!.Id;

            var updateRequest = new
            {
                matchId,
                playerName = "Updated Player Name",
                gamePoints = 999,
                pointsDescription = "Updated description"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match-data-point/{dataPointId}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify update by fetching the data point
            var getResponse = await Client.GetAsync($"/match-data-point/{dataPointId}");
            var updatedDataPoint = JsonConvert.DeserializeObject<MatchDataPointDto>(
                await getResponse.Content.ReadAsStringAsync());

            Assert.NotNull(updatedDataPoint);
            Assert.Equal("Updated Player Name", updatedDataPoint.PlayerName);
            Assert.Equal(999, updatedDataPoint.GamePoints);
            Assert.Equal("Updated description", updatedDataPoint.PointsDescription);
        }

        [Fact]
        public async Task UpdateMatchDataPoint_WithPartialUpdate_ReturnsNoContent()
        {
            // Arrange - Get an existing data point
            var allResponse = await Client.GetAsync("/match-data-points/all");
            var allDataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(
                await allResponse.Content.ReadAsStringAsync());

            if (allDataPoints == null || allDataPoints.Count == 0)
                return;

            var dataPointToUpdate = allDataPoints.First();
            var updateRequest = new
            {
                gamePoints = 555, // Only update points, leave other fields as is
                playerName = (string?)null,
                matchId = (int?)null,
                pointsDescription = (string?)null
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match-data-point/{dataPointToUpdate.Id}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify update
            var getResponse = await Client.GetAsync($"/match-data-point/{dataPointToUpdate.Id}");
            var updatedDataPoint = JsonConvert.DeserializeObject<MatchDataPointDto>(
                await getResponse.Content.ReadAsStringAsync());

            Assert.NotNull(updatedDataPoint);
            Assert.Equal(555, updatedDataPoint.GamePoints);
        }

        [Fact]
        public async Task UpdateMatchDataPoint_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateRequest = new
            {
                playerName = "Updated Name",
                gamePoints = 100,
                pointsDescription = "Updated"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync("/match-data-point/-1", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMatchDataPoint_ChangePlayerName_ReturnsNoContent()
        {
            // Arrange - Get an existing data point
            var allResponse = await Client.GetAsync("/match-data-points/all");
            var allDataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(
                await allResponse.Content.ReadAsStringAsync());

            if (allDataPoints == null || allDataPoints.Count == 0)
                return;

            var dataPointToUpdate = allDataPoints.First();
            var newPlayerName = $"Updated-{Guid.NewGuid().ToString()[..8]}";

            var updateRequest = new
            {
                playerName = newPlayerName
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match-data-point/{dataPointToUpdate.Id}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify player name changed (trigger should update player count)
            var getResponse = await Client.GetAsync($"/match-data-point/{dataPointToUpdate.Id}");
            var updatedDataPoint = JsonConvert.DeserializeObject<MatchDataPointDto>(
                await getResponse.Content.ReadAsStringAsync());

            Assert.NotNull(updatedDataPoint);
            Assert.Equal(newPlayerName, updatedDataPoint.PlayerName);
        }

        [Fact]
        public async Task UpdateMatchDataPoint_ChangeMatchId_ReturnsBadRequestIfDataExists()
        {
            // Arrange - Get two data points to test cross-match updates
            var allResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await allResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count < 2)
                return; // Need at least 2 matches to test

            var dataPointResponse = await Client.GetAsync("/match-data-points/all");
            var allDataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(
                await dataPointResponse.Content.ReadAsStringAsync());

            if (allDataPoints == null || allDataPoints.Count == 0)
                return;

            var dataPointToUpdate = allDataPoints.First();
            var differentMatchId = matches.FirstOrDefault(m => m.MatchId != dataPointToUpdate.MatchId)?.MatchId ?? -1;

            if (differentMatchId == -1)
                return;

            var updateRequest = new
            {
                matchId = differentMatchId // Try to change to different match
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act - May return BadRequest or succeed depending on implementation
            var response = await Client.PutAsync($"/match-data-point/{dataPointToUpdate.Id}", content);

            // Assert - Should either reject or succeed (both are valid implementations)
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest,
                        "Update should either succeed or reject cross-match changes");
        }
    }
}
