using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchPutTests : TestBase
    {
        [Fact]
        public async Task UpdateMatch_WithValidData_ReturnsNoContent()
        {
            // Arrange - Create a match
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameId = (int)games!.First().id;
            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Original notes", isFinished = false };
            var createResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);
            var created = JsonConvert.DeserializeObject<MatchForMatchDto>(await createResponse.Content.ReadAsStringAsync());
            var matchId = created!.MatchId;

            var updateRequest = new
            {
                gameId,
                matchDate = DateTime.UtcNow.AddHours(1),
                notes = "Updated notes",
                isFinished = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match/{matchId}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify update by fetching the match
            var getResponse = await Client.GetAsync($"/match/{matchId}");
            var updatedMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await getResponse.Content.ReadAsStringAsync());

            Assert.NotNull(updatedMatch);
            Assert.True(updatedMatch.isFinished);
            Assert.Equal("Updated notes", updatedMatch.Notes);
        }

        [Fact]
        public async Task UpdateMatch_WithDifferentGameId_ReturnsBadRequest()
        {
            // Arrange - Get an existing match
            var matchesResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchToUpdate = matches.First();

            // Get a different game ID
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());

            var differentGameId = games?.FirstOrDefault(g => g.Id != matchToUpdate.GameId)?.Id ?? -999;

            var updateRequest = new
            {
                gameId = differentGameId, // Different game - should fail
                matchDate = DateTime.UtcNow,
                notes = "Updated notes",
                isFinished = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match/{matchToUpdate.MatchId}", content);

            // Assert - Should return 400 or 409 (constraint violation)
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                        response.StatusCode == System.Net.HttpStatusCode.Conflict,
                        "Should not allow changing GameId for existing match with data points");
        }

        [Fact]
        public async Task UpdateMatch_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateRequest = new
            {
                gameId = 1,
                matchDate = DateTime.UtcNow,
                notes = "Updated notes",
                isFinished = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync("/match/-1", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMatch_ClearNotes_ReturnsNoContent()
        {
            // Arrange - Get an existing match
            var matchesResponse = await Client.GetAsync("/matches");
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchToUpdate = matches.First();
            var updateRequest = new
            {
                gameId = matchToUpdate.GameId,
                matchDate = matchToUpdate.MatchDate,
                notes = (string)null, // Clear notes
                isFinished = matchToUpdate.isFinished
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(updateRequest),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PutAsync($"/match/{matchToUpdate.MatchId}", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

            // Verify notes are cleared
            var getResponse = await Client.GetAsync($"/match/{matchToUpdate.MatchId}");
            var updatedMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await getResponse.Content.ReadAsStringAsync());

            Assert.NotNull(updatedMatch);
            Assert.Null(updatedMatch.Notes);
        }
    }
}
