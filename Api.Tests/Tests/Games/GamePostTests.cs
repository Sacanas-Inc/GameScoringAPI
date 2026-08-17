using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;

namespace Api.Tests
{
    public class GamePostTests :TestBase
    {
        public GamePostTests()
        {
        }

        [Fact]
        public async Task CreateSingleGame_ReturnsCreated()
        {            
            var result = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = "UnitTestName",
                GameDescription = "This is a test game description."
            });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task CreateSingleGame_BadRequest_InvalidData()
        {
            // Test for invalid game Name.
            var result = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = ""
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            // Test for invalid MinPlayers.
            result = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = "TESTE", MinPlayers = -1
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            
            // Test for invalid MaxPlayers.
            result = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = "TESTE2", MinPlayers = -1
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            // Test for invalid AverageDuration.
            result = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = "TESTE2", AverageDuration = -1
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateSingleGame_ReturnsCorrectData()
        {
            // Create the game DTO to send in the POST request
            var gameToCreate = new CreateGameRequest
            {
                GameName = "GameTest",
                GameDescription = "This is a test game description.",
                MinPlayers = 2,
                MaxPlayers = 6,
                AverageDuration = 90
            };

            // Send the POST request
            var result = await Client.PostAsJsonAsync("/game", gameToCreate);

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
            var result = await Client.PostAsJsonAsync("/games", new List<CreateGameRequest>
            {
                new CreateGameRequest { GameName = "Game 1" }, 
                new CreateGameRequest { GameName = "Game 2" }, 
                new CreateGameRequest { GameName = "Game 3" }
            });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task CreateMultipleGames_BadRequest_InvalidData()
        {           
            var result = await Client.PostAsJsonAsync("/games", new List<CreateGameRequest>
            {
                new CreateGameRequest { GameName = "Game 1" }, 
                new CreateGameRequest { GameName = "" }, 
                new CreateGameRequest { GameName = "Game 3" }
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            
            result = await Client.PostAsJsonAsync("/games", new List<CreateGameRequest>
            {
                new CreateGameRequest { GameName = "Game 1", MinPlayers = 0}, 
                new CreateGameRequest { GameName = "Game 2",  MinPlayers = -1 }, 
                new CreateGameRequest { GameName = "Game 3", MinPlayers = 5 }
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            result = await Client.PostAsJsonAsync("/games", new List<CreateGameRequest>
            {
                new CreateGameRequest { GameName = "Game 1"}, 
                new CreateGameRequest { GameName = "Game 2",  AverageDuration = -1 }, 
                new CreateGameRequest { GameName = "Game 3"}
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateMultipleGames_ReturnsCorrectData()
        {
            // Create the list of game DTOs to send in the POST request
            var gamesToCreate = new List<CreateGameRequest>
            {
                new CreateGameRequest
                {
                    GameName = "GameTest1",
                    GameDescription = "This is a test game description 1.",
                    MinPlayers = 2,
                    MaxPlayers = 6,
                    AverageDuration = 90
                },
                new CreateGameRequest
                {
                    GameName = "GameTest2",
                    GameDescription = "This is a test game description 2.",
                    MinPlayers = 3,
                    MaxPlayers = 5,
                    AverageDuration = 60
                },
                new CreateGameRequest
                {
                    GameName = "GameTest3",
                    GameDescription = "This is a test game description 3.",
                    MinPlayers = 1,
                    MaxPlayers = 4,
                    AverageDuration = 30
                }
            };

            // Send the POST request
            var result = await Client.PostAsJsonAsync("/games", gamesToCreate);

            // Log the status code
            Console.WriteLine($"Status Code: {result.StatusCode}");

            // Read and log the raw response content
            var jsonResponseString = await result.Content.ReadAsStringAsync();
            Console.WriteLine("Response JSON: " + jsonResponseString);

            // Deserialize the response content
            var createdGames = JsonConvert.DeserializeObject<List<GameDto>>(jsonResponseString);

            // Assert that the number of created games matches the number of games sent
            Assert.Equal(gamesToCreate.Count, createdGames.Count);

            for (int i = 0; i < gamesToCreate.Count; i++)
            {
                var sentGame = gamesToCreate[i];
                var createdGame = createdGames[i];

                // Assert that each created game matches the sent game
                Assert.NotEqual(0, createdGame.Id); // Assert ID was generated
                Assert.Equal(sentGame.GameName, createdGame.GameName);
                Assert.Equal(sentGame.GameDescription, createdGame.GameDescription);
                Assert.Equal(sentGame.MinPlayers, createdGame.MinPlayers);
                Assert.Equal(sentGame.MaxPlayers, createdGame.MaxPlayers);
                Assert.Equal(sentGame.AverageDuration, createdGame.AverageDuration);
                Assert.Equal(0, createdGame.MatchesCount); // New games should have 0 matches
            }
        
        }
    
        [Fact]
        public async Task CreateSingleGame_ResponseHeadersContainExpectedValues()
        {
            // Act
            var response = await Client.PostAsJsonAsync("/game", new CreateGameRequest
            {
                GameName = "UnitTestName"
            });
            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
        [Fact]
        public async Task CreateMultipleGames_ResponseHeadersContainExpectedValues()
        {
            // Create the list of game DTOs to send in the POST request
            var gamesToCreate = new List<CreateGameRequest>
            {
                new CreateGameRequest
                {
                    GameName = "GameTest1",
                    GameDescription = "This is a test game description 1.",
                    MinPlayers = 2,
                    MaxPlayers = 6,
                    AverageDuration = 90
                }
            };

            // Send the POST request
            var response = await Client.PostAsJsonAsync("/games", gamesToCreate);
            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
    }
}