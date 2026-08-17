using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchDataPointGetTests : TestBase
    {
        [Fact]
        public async Task GetAllMatchDataPoints_ReturnsAllDataPoints()
        {
            // Act
            var response = await Client.GetAsync("/match-data-points/all");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var dataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dataPoints);
        }

        [Fact]
        public async Task GetAllMatchDataPointsDetailed_ReturnsDetailedDataPoints()
        {
            // Act
            var response = await Client.GetAsync("/match-data-points/all/detailed");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var dataPoints = JsonConvert.DeserializeObject<List<object>>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dataPoints);
        }

        [Fact]
        public async Task GetMatchDataPointById_WithExistingId_ReturnsDataPoint()
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

            var dpRequest = new { matchId, playerName = "Player1", gamePoints = 100, pointsDescription = "Test" };
            var dpResponse = await Client.PostAsync($"/match-data-point/{matchId}",
                new StringContent(JsonConvert.SerializeObject(dpRequest), Encoding.UTF8, "application/json"));
            Assert.Equal(System.Net.HttpStatusCode.Created, dpResponse.StatusCode);
            var createdDp = JsonConvert.DeserializeObject<dynamic>(await dpResponse.Content.ReadAsStringAsync());
            var dataPointId = (int)createdDp!.id;

            // Act
            var response = await Client.GetAsync($"/match-data-point/{dataPointId}");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var dataPoint = JsonConvert.DeserializeObject<MatchDataPointDto>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dataPoint);
            Assert.Equal(dataPointId, dataPoint.Id);
        }

        [Fact]
        public async Task GetMatchDataPointsByMatchId_ReturnsDataPointsForMatch()
        {
            // Arrange - Create a match
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

            // Act
            var response = await Client.GetAsync($"/match/{matchId}/data-points");
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var dataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dataPoints);
        }

        [Fact]
        public async Task GetMatchDataPointById_WithNonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync("/match-data-point/-1");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMatchDataPoint_ResponseHeadersContainExpectedValues()
        {
            // Arrange - Get first data point
            var allResponse = await Client.GetAsync("/match-data-points/all");
            var allDataPoints = JsonConvert.DeserializeObject<List<MatchDataPointDto>>(
                await allResponse.Content.ReadAsStringAsync());

            if (allDataPoints == null || allDataPoints.Count == 0)
                return;

            // Act
            var response = await Client.GetAsync($"/match-data-point/{allDataPoints.First().Id}");

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
    }

    public class MatchDataPointDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("matchId")]
        public int MatchId { get; set; }

        [JsonProperty("playerName")]
        public string PlayerName { get; set; }

        [JsonProperty("gamePoints")]
        public int GamePoints { get; set; }

        [JsonProperty("pointsDescription")]
        public string PointsDescription { get; set; }

        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; }
    }
}
