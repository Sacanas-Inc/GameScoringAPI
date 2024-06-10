using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace Api.Tests
{
    public class GamePostTests
    {

        [Fact]
        public async Task CreateSingleGame_ReturnsCreated()
        {
            await using var application = new WebApplicationFactory<Program>();

            var client = application.CreateClient();
            
            var result = await client.PostAsJsonAsync("/game", new GameDto
            {
                GameName = "UnitTestName",
                GameDescription = "This is a test game description."
            });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task CreateSingleGame_BadRequest_InvalidData()
        {
            await using var application = new WebApplicationFactory<Program>();

            var client = application.CreateClient();
            
            // Test for invalid game Name.
            var result = await client.PostAsJsonAsync("/game", new GameDto
            {
                GameName = ""
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            // Test for invalid MinPlayers.
            result = await client.PostAsJsonAsync("/game", new GameDto
            {
                GameName = "TESTE",
                MinPlayers = -1
                
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            
            // Test for invalid MaxPlayers.
            result = await client.PostAsJsonAsync("/game", new GameDto
            {
                GameName = "TESTE2",
                MinPlayers = -1
                
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            // Test for invalid AverageDuration.
            result = await client.PostAsJsonAsync("/game", new GameDto
            {
                GameName = "TESTE2",
                AverageDuration = -1
                
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateSingleGame_ReturnsCorrectData()
        {
            await using var application = new WebApplicationFactory<Program>();
            var client = application.CreateClient();

            // Create the game DTO to send in the POST request
            var gameToCreate = new GameDto
            {
                GameName = "GameTest",
                GameDescription = "This is a test game description.",
                MinPlayers = 2,
                MaxPlayers = 6,
                AverageDuration = 90,
                MatchesCount = 0
            };

            // Send the POST request
            var result = await client.PostAsJsonAsync("/game", gameToCreate);

            // Read and deserialize the response content
            var jsonResponseString = await result.Content.ReadAsStringAsync();
            var createdGame = Newtonsoft.Json.JsonConvert.DeserializeObject<GameDto>(jsonResponseString);

            // Create the expected game DTO with the expected Id
            var expectedGame = new GameDto
            {
                Id = createdGame.Id,  // Assume the Id is set by the server and is not known beforehand
                GameName = "GameTest",
                GameDescription = "This is a test game description.",
                MinPlayers = 2,
                MaxPlayers = 6,
                AverageDuration = 90,
                MatchesCount = 0
            };

            // Assert that the created game matches the expected game
            Assert.Equal(expectedGame.Id, createdGame.Id);
            Assert.Equal(expectedGame.GameName, createdGame.GameName);
            Assert.Equal(expectedGame.GameDescription, createdGame.GameDescription);
            Assert.Equal(expectedGame.MinPlayers, createdGame.MinPlayers);
            Assert.Equal(expectedGame.MaxPlayers, createdGame.MaxPlayers);
            Assert.Equal(expectedGame.AverageDuration, createdGame.AverageDuration);
            Assert.Equal(expectedGame.MatchesCount, createdGame.MatchesCount);
        }

        [Fact]
        public async Task CreateMultipleGames_ReturnsCreated()
        {
            await using var application = new WebApplicationFactory<Program>();

            var client = application.CreateClient();
            
            var result = await client.PostAsJsonAsync("/games", new List<GameDto>
            {
                new GameDto { GameName = "Game 1" },
                new GameDto { GameName = "Game 2" },
                new GameDto { GameName = "Game 3" }
            });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }


        // TODO: Adapt to multiple.
        [Fact]
        public async Task CreateMultipleGames_BadRequest_InvalidData()
        {
        }

    }
}