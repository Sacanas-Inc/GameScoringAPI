using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchGetTests : TestBase
    {
        [Fact]
        public async Task GetAllMatches_ReturnsAllMatches()
        {
            // Arrange - Create a match so the list is non-empty
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameId = (int)games!.First().id;
            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Test", isFinished = false };
            await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));

            // Act
            var response = await Client.GetAsync("/matches");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(matches);
            Assert.NotEmpty(matches);
        }

        [Fact]
        public async Task GetAllMatches_ByGameId_ReturnsFilteredMatches()
        {
            // Arrange - Create a match for a known game
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameIdToFilter = (int)games!.First().id;
            var matchRequest = new { gameId = gameIdToFilter, matchDate = DateTime.UtcNow, notes = "Test", isFinished = false };
            await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));

            // Act - Get matches for specific game
            var response = await Client.GetAsync($"/matches?gameId={gameIdToFilter}");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var filteredMatches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(filteredMatches);
            Assert.NotEmpty(filteredMatches);
            foreach (var match in filteredMatches)
            {
                Assert.Equal(gameIdToFilter, match.GameId);
            }
        }

        [Fact]
        public async Task GetMatchById_WithExistingId_ReturnsMatch()
        {
            // Arrange - Create a match
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameId = (int)games!.First().id;
            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Test", isFinished = false };
            var createResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);
            var created = JsonConvert.DeserializeObject<MatchForMatchDto>(await createResponse.Content.ReadAsStringAsync());
            var matchId = created!.MatchId;

            // Act
            var response = await Client.GetAsync($"/match/{matchId}");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var match = JsonConvert.DeserializeObject<MatchForMatchDto>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(match);
            Assert.Equal(matchId, match.MatchId);
        }

        [Fact]
        public async Task GetMatchById_WithExistingId_IncludeDataPoints_ReturnsMatchWithDataPoints()
        {
            // Arrange - Create a match
            var gamesResponse = await Client.GetAsync("/games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(await gamesResponse.Content.ReadAsStringAsync());
            var gameId = (int)games!.First().id;
            var matchRequest = new { gameId, matchDate = DateTime.UtcNow, notes = "Test", isFinished = false };
            var createResponse = await Client.PostAsync("/match",
                new StringContent(JsonConvert.SerializeObject(matchRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);
            var created = JsonConvert.DeserializeObject<MatchForMatchDto>(await createResponse.Content.ReadAsStringAsync());
            var matchId = created!.MatchId;

            // Act - Get match with data points
            var response = await Client.GetAsync($"/match/{matchId}?includeDataPoints=true");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var match = JsonConvert.DeserializeObject<MatchForMatchDto>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(match);
            Assert.Equal(matchId, match.MatchId);
            Assert.NotNull(match.MatchDataPoints);
        }

        [Fact]
        public async Task GetMatchById_WithNonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync("/match/-1");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);
            Assert.Contains("not found", jsonResponse["error"].ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetMatch_ResponseHeadersContainExpectedValues()
        {
            // Arrange - Get first match
            var allResponse = await Client.GetAsync("/matches");
            var allMatches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await allResponse.Content.ReadAsStringAsync());

            if (allMatches == null || allMatches.Count == 0)
                return;

            // Act
            var response = await Client.GetAsync($"/match/{allMatches.First().MatchId}");

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
    }
}
