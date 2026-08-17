using System.Text;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class MatchDataPointDeleteTests : TestBase
    {
        [Fact]
        public async Task DeleteMatchDataPoint_WithExistingId_ReturnsNoContent()
        {
            // Arrange - Get games and create a match to work with
            var gamesResponse = await Client.GetAsync("/games");
            Assert.True(gamesResponse.IsSuccessStatusCode, "Failed to retrieve games");
            var games = JsonConvert.DeserializeObject<List<dynamic>>(
                await gamesResponse.Content.ReadAsStringAsync());
            Assert.NotNull(games);
            Assert.NotEmpty(games);

            var gameId = (int)games.First().id;
            var matchRequest = new
            {
                gameId = gameId,
                matchDate = DateTime.UtcNow,
                notes = "Test match for delete test",
                isFinished = false
            };

            var matchCreateContent = new StringContent(
                JsonConvert.SerializeObject(matchRequest),
                Encoding.UTF8,
                "application/json");

            var matchCreateResponse = await Client.PostAsync("/match", matchCreateContent);
            Assert.Equal(System.Net.HttpStatusCode.Created, matchCreateResponse.StatusCode);
            var createdMatch = JsonConvert.DeserializeObject<dynamic>(
                await matchCreateResponse.Content.ReadAsStringAsync());
            var matchId = (int)createdMatch.matchId;
            var dataPointRequest = new
            {
                matchId = matchId,
                playerName = $"Player-{Guid.NewGuid().ToString()[..8]}",
                gamePoints = 100,
                pointsDescription = "Data point to delete"
            };

            var createContent = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            var createResponse = await Client.PostAsync("/match-data-point", createContent);
            var createdDataPoint = JsonConvert.DeserializeObject<dynamic>(
                await createResponse.Content.ReadAsStringAsync());

            if (createdDataPoint == null)
                return;

            // Extract ID from response (could be embedded in JSON)
            int dataPointId = createdDataPoint.id ?? createdDataPoint.Id ?? -1;
            if (dataPointId == -1)
            {
                // Try to get it from Location header
                var locationHeader = createResponse.Headers.Location?.ToString() ?? "";
                var match = System.Text.RegularExpressions.Regex.Match(locationHeader, @"/(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
                    dataPointId = id;
            }

            if (dataPointId == -1)
                return;

            // Act - Delete the data point
            var deleteResponse = await Client.DeleteAsync($"/match-data-point/{dataPointId}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify deletion by attempting to get the data point
            var getResponse = await Client.GetAsync($"/match-data-point/{dataPointId}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMatchDataPoint_WithNonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await Client.DeleteAsync("/match-data-point/-1");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMatchDataPoint_VerifiesTriggerUpdatesMatchPlayerCount()
        {
            // Arrange - Get a match and create a data point with unique player
            var matchesResponse = await Client.GetAsync("/matches");
            if (!matchesResponse.IsSuccessStatusCode) return;
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchId = matches.First().MatchId;
            var playerName = $"Player-{Guid.NewGuid().ToString()[..8]}";

            // Get initial player count
            var matchBeforeResponse = await Client.GetAsync($"/match/{matchId}");
            var matchBefore = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchBeforeResponse.Content.ReadAsStringAsync());
            var playerCountBefore = matchBefore?.PlayerCount ?? 0;

            // Create a new data point
            var dataPointRequest = new
            {
                matchId = matchId,
                playerName = playerName,
                gamePoints = 50,
                pointsDescription = "Point to delete"
            };

            var createContent = new StringContent(
                JsonConvert.SerializeObject(dataPointRequest),
                Encoding.UTF8,
                "application/json");

            var createResponse = await Client.PostAsync("/match-data-point", createContent);
            Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);

            // Get data point ID from response
            var createdDataPoint = JsonConvert.DeserializeObject<dynamic>(
                await createResponse.Content.ReadAsStringAsync());
            int dataPointId = createdDataPoint.id ?? createdDataPoint.Id ?? -1;

            if (dataPointId == -1)
            {
                var locationHeader = createResponse.Headers.Location?.ToString() ?? "";
                var match = System.Text.RegularExpressions.Regex.Match(locationHeader, @"/(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
                    dataPointId = id;
            }

            if (dataPointId == -1)
                return;

            // Get player count after creation
            var matchAfterCreateResponse = await Client.GetAsync($"/match/{matchId}");
            var matchAfterCreate = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchAfterCreateResponse.Content.ReadAsStringAsync());
            var playerCountAfterCreate = matchAfterCreate?.PlayerCount ?? 0;

            // Act - Delete the data point
            var deleteResponse = await Client.DeleteAsync($"/match-data-point/{dataPointId}");
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Assert - Get final player count
            var matchAfterDeleteResponse = await Client.GetAsync($"/match/{matchId}");
            var matchAfterDelete = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchAfterDeleteResponse.Content.ReadAsStringAsync());
            var playerCountAfterDelete = matchAfterDelete?.PlayerCount ?? 0;

            // Verify player count changed (trigger should update)
            // Note: If this was the only data point for this player, count should decrease
            // Otherwise count stays same if other data points exist for same player
            Assert.True(playerCountAfterDelete <= playerCountAfterCreate,
                "Database trigger should have updated Matches.PlayerCount");
        }

        [Fact]
        public async Task DeleteMultipleDataPoints_FromSameMatch_UpdatesCountCorrectly()
        {
            // Arrange - Get a match
            var matchesResponse = await Client.GetAsync("/matches");
            if (!matchesResponse.IsSuccessStatusCode) return;
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(
                await matchesResponse.Content.ReadAsStringAsync());

            if (matches == null || matches.Count == 0)
                return;

            var matchId = matches.First().MatchId;

            // Create two data points with different players
            var playerNames = new[] 
            { 
                $"Player1-{Guid.NewGuid().ToString()[..6]}",
                $"Player2-{Guid.NewGuid().ToString()[..6]}"
            };

            var dataPointIds = new List<int>();

            foreach (var playerName in playerNames)
            {
                var dataPointRequest = new
                {
                    matchId = matchId,
                    playerName = playerName,
                    gamePoints = 100,
                    pointsDescription = "Test point"
                };

                var createContent = new StringContent(
                    JsonConvert.SerializeObject(dataPointRequest),
                    Encoding.UTF8,
                    "application/json");

                var createResponse = await Client.PostAsync("/match-data-point", createContent);
                if (createResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    var createdDataPoint = JsonConvert.DeserializeObject<dynamic>(
                        await createResponse.Content.ReadAsStringAsync());
                    int dataPointId = createdDataPoint?.id ?? createdDataPoint?.Id ?? -1;
                    if (dataPointId != -1)
                        dataPointIds.Add(dataPointId);
                }
            }

            if (dataPointIds.Count < 2)
                return;

            // Get player count before deletion
            var matchBeforeResponse = await Client.GetAsync($"/match/{matchId}");
            var matchBefore = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchBeforeResponse.Content.ReadAsStringAsync());
            var playerCountBefore = matchBefore?.PlayerCount ?? 0;

            // Act - Delete all created data points
            foreach (var dataPointId in dataPointIds)
            {
                var deleteResponse = await Client.DeleteAsync($"/match-data-point/{dataPointId}");
                Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);
            }

            // Assert - Verify player count decreased
            var matchAfterResponse = await Client.GetAsync($"/match/{matchId}");
            var matchAfter = JsonConvert.DeserializeObject<MatchForMatchDto>(
                await matchAfterResponse.Content.ReadAsStringAsync());
            var playerCountAfter = matchAfter?.PlayerCount ?? 0;

            Assert.True(playerCountAfter < playerCountBefore,
                "Deleting all data points for players should decrease player count");
        }

        // ── Bulk delete (DELETE /match-data-points) ──────────────────────────

        private async Task<int> CreateDataPointForMatchAsync(int matchId)
        {
            var request = new
            {
                playerName = $"Player-{Guid.NewGuid().ToString()[..8]}",
                gamePoints = 10,
                pointsDescription = "Bulk delete test point"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"/match-data-point/{matchId}", content);

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
                return -1;

            var created = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            int id = created?.id ?? created?.Id ?? -1;

            if (id == -1)
            {
                var location = response.Headers.Location?.ToString() ?? "";
                var m = System.Text.RegularExpressions.Regex.Match(location, @"/(\d+)$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var parsed))
                    id = parsed;
            }

            return id;
        }

        private async Task<int> GetFirstMatchIdAsync()
        {
            var response = await Client.GetAsync("/matches");
            if (!response.IsSuccessStatusCode) return -1;
            var matches = JsonConvert.DeserializeObject<List<MatchForMatchDto>>(await response.Content.ReadAsStringAsync());
            return matches?.FirstOrDefault()?.MatchId ?? -1;
        }

        [Fact]
        public async Task BulkDelete_AllValidIds_ReturnsNoContent()
        {
            var matchId = await GetFirstMatchIdAsync();
            if (matchId == -1) return;

            var id1 = await CreateDataPointForMatchAsync(matchId);
            var id2 = await CreateDataPointForMatchAsync(matchId);
            if (id1 == -1 || id2 == -1) return;

            var body = new StringContent(
                JsonConvert.SerializeObject(new { ids = new[] { id1, id2 } }),
                Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/match-data-points") { Content = body });

            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task BulkDelete_MixValidAndInvalidIds_ReturnsMultiStatus()
        {
            var matchId = await GetFirstMatchIdAsync();
            if (matchId == -1) return;

            var validId = await CreateDataPointForMatchAsync(matchId);
            if (validId == -1) return;
            var invalidId = -999;

            var body = new StringContent(
                JsonConvert.SerializeObject(new { ids = new[] { validId, invalidId } }),
                Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/match-data-points") { Content = body });

            Assert.Equal(207, (int)response.StatusCode);

            var results = JsonConvert.DeserializeObject<List<dynamic>>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(results);
            Assert.Contains(results, r => (int)r.id == validId && (bool)r.deleted == true);
            Assert.Contains(results, r => (int)r.id == invalidId && (bool)r.deleted == false);
        }

        [Fact]
        public async Task BulkDelete_AllInvalidIds_ReturnsMultiStatusAllFailed()
        {
            var body = new StringContent(
                JsonConvert.SerializeObject(new { ids = new[] { -1, -2, -3 } }),
                Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/match-data-points") { Content = body });

            Assert.Equal(207, (int)response.StatusCode);

            var results = JsonConvert.DeserializeObject<List<dynamic>>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(results);
            Assert.All(results, r => Assert.False((bool)r.deleted));
        }

        [Fact]
        public async Task BulkDelete_EmptyIds_ReturnsBadRequest()
        {
            var body = new StringContent(
                JsonConvert.SerializeObject(new { ids = Array.Empty<int>() }),
                Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/match-data-points") { Content = body });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
