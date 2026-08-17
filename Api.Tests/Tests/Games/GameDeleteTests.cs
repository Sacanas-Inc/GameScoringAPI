using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Api.Tests
{
    public class GameDeleteTests :TestBase
    {
        public GameDeleteTests()
        {
        }
        
        [Fact]
        public async Task DeleteGameById_DeletesCorrectEntity()
        {
            int gameId = 2; // Change to a valid game ID for your test
            var responseDel = await Client.DeleteAsync($"/game/{gameId}");
            var responseGet = await Client.GetAsync($"/game/{gameId}");
            // Read the response content as a string
            var jsonResponseString = await responseGet.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(jsonResponseString);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, responseDel.StatusCode);
            // Assert that the status code is 404 Not Found
            Assert.Equal(System.Net.HttpStatusCode.NotFound, responseGet.StatusCode);
            // Assert that the error message contains the expected text
            Assert.Contains($"Game with ID {gameId.ToString()} not found.", jsonResponse["error"].ToString());
        }

    }
}