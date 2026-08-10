using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace Api.Tests
{
    public class MatchDeleteTests : TestBase
    {
        [Fact]
        public async Task DeleteMatch_WithExistingId_ReturnsNoContent()
        {
            // Arrange - Create a new match first
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());

            if (games == null || games.Count == 0)
            {
                Assert.True(false, "No games in database to test match deletion");
                return;
            }

            var gameId = games.First().Id;
            var matchRequest = new
            {
                gameId = gameId,
                matchDate = DateTime.UtcNow,
                notes = "Match to delete",
                isFinished = false
            };

            var createContent = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var createResponse = await Client.PostAsync("/match", createContent);
            var createdMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await createResponse.Content.ReadAsStringAsync());

            if (createdMatch == null)
                return;

            // Act - Delete the match
            var deleteResponse = await Client.DeleteAsync($"/match/{createdMatch.MatchId}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify deletion by attempting to get the match
            var getResponse = await Client.GetAsync($"/match/{createdMatch.MatchId}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMatch_WithNonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await Client.DeleteAsync("/match/-1");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMatch_CascadesDataPointDeletion()
        {
            // Arrange - Create a match with a data point
            var gamesResponse = await Client.GetAsync("/games");
            Assert.True(gamesResponse.IsSuccessStatusCode, "Failed to retrieve games");
            var games = JsonConvert.DeserializeObject<List<GameDto>>(
                await gamesResponse.Content.ReadAsStringAsync());
            Assert.NotNull(games);
            var gameId = games.First().Id;

            var matchRequest = new
            {
                gameId,
                matchDate = DateTime.UtcNow,
                notes = "Match for cascade test",
                isFinished = false
            };
            var matchResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, matchResponse.StatusCode);
            var createdMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchResponse.Content.ReadAsStringAsync());
            var matchId = createdMatch!.MatchId;

            var dpRequest = new { matchId, playerName = "Player1", gamePoints = 50, pointsDescription = "Test" };
            var dpResponse = await Client.PostAsync($"/match-data-point/{matchId}",
                new StringContent(JsonConvert.SerializeObject(dpRequest), System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, dpResponse.StatusCode);
            var createdDp = JsonConvert.DeserializeObject<dynamic>(await dpResponse.Content.ReadAsStringAsync());
            var dataPointId = (int)createdDp!.id;

            // Act - Delete the match
            var deleteResponse = await Client.DeleteAsync($"/match/{matchId}");

            // Assert deletion succeeded
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify match is deleted
            var getResponse = await Client.GetAsync($"/match/{matchId}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);

            // Verify data point cascade deleted
            var dataPointResponse = await Client.GetAsync($"/match-data-point/{dataPointId}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, dataPointResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMatch_VerifiesTriggerUpdatesGameMatchCount()
        {
            // Arrange - Create a new match
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
                notes = "Match for trigger test",
                isFinished = false
            };

            var createContent = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var createResponse = await Client.PostAsync("/match", createContent);
            var createdMatch = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await createResponse.Content.ReadAsStringAsync());

            if (createdMatch == null)
                return;

            // Get updated count after creation
            var gameAfterCreateResponse = await Client.GetAsync($"/game/{gameId}");
            var gameAfterCreate = JsonConvert.DeserializeObject<GameDto>(
                await gameAfterCreateResponse.Content.ReadAsStringAsync());
            var matchCountAfterCreate = gameAfterCreate?.MatchesCount ?? 0;

            // Act - Delete the match
            var deleteResponse = await Client.DeleteAsync($"/match/{createdMatch.MatchId}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Get final match count
            var gameAfterDeleteResponse = await Client.GetAsync($"/game/{gameId}");
            var gameAfterDelete = JsonConvert.DeserializeObject<GameDto>(
                await gameAfterDeleteResponse.Content.ReadAsStringAsync());
            var matchCountAfterDelete = gameAfterDelete?.MatchesCount ?? 0;

            // Verify match count decreased (trigger should fire)
            Assert.True(matchCountAfterDelete < matchCountAfterCreate,
                "Database trigger should have decremented Games.MatchesCount");
        }
    }
}
