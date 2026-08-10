using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class GameGetTests :TestBase
    {
        public GameGetTests()
        {
        }
        
        [Fact]
        public async Task GetGameById_ReturnsGameIfExists()
        {
            int gameId = 1; // Change to a valid game ID for your test
            var response = await Client.GetAsync($"/game/{gameId}");

            // Read the response content as a string
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            // Parse the JSON response into a JObject
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            // Assert specific properties or values in the parsed JSON object
            Assert.Equal(gameId, jsonResponse["id"]);
            Assert.Equal("Example Game Name", jsonResponse["gameName"]);
        }

        [Fact]
        public async Task GetGameById_ReturnsNotFound()
        {
            int gameId = -1; // Use an ID that you know will not exist
            var response = await Client.GetAsync($"/game/{gameId}");

            // Read the response content as a string
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);
            
            // Assert that the status code is 404 Not Found
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            // Assert that the error message contains the expected text
            Assert.Contains($"Game with ID {gameId.ToString()} not found.", jsonResponse["error"].ToString());
        }

        [Fact]
        public async Task GetGamesById_ReturnsGameIfExists()
        {
            int requestedGameId = 1; // Change to a valid game ID for your test
            var response = await Client.GetAsync($"/games?id={requestedGameId}");
            // Read the response content as a string
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            // Parse the JSON response into a JObject
            var gottenGames = JsonConvert.DeserializeObject<List<GameDto>>(jsonResponseString);

            // Asserts
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Single(gottenGames);
            if(gottenGames is not null)
                foreach(GameDto game in gottenGames)
                    Assert.Equal(requestedGameId, game.Id);

        }

        [Fact]
        public async Task GetGamesByDescription_ReturnsGameIfExists()
        {
            string gameDescription = "cartas"; // Change to a valid game ID for your test
            var response = await Client.GetAsync($"/games?description={gameDescription}");
            // Read the response content as a string
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            // Parse the JSON response into a JObject
            var gottenGames = JsonConvert.DeserializeObject<List<GameDto>>(jsonResponseString);

            // Asserts
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(gottenGames);
            if(gottenGames is not null)
                foreach(GameDto game in gottenGames)
                    Assert.Contains(gameDescription, game.GameDescription);
        }

        [Fact]
        public async Task GetGamesById_ReturnsNotFound()
        {
            int gameId = -1; // Use an ID that you know will not exist
            var response = await Client.GetAsync($"/games?id={gameId}");

            // Read the response content as a string
            var jsonResponseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);
            
            // Assert that the status code is 404 Not Found
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            // Assert that the error message contains the expected text
            Assert.Contains($"No games found with provided params.", jsonResponse["error"].ToString());
        }

        [Fact]
        public async Task GetGame_ResponseHeadersContainExpectedValues()
        {
            // Act
            var response = await Client.GetAsync("/game/1");

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task GetGames_ResponseHeadersContainExpectedValues()
        {
            // Act
            var response = await Client.GetAsync("/games");

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
    }
}