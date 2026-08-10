using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class PostGameEndpoints
{
    public static void MapPostGameEndpoints(this WebApplication app)
    {
        
        app.MapPost("/game", async (CreateGameRequest request, IGameService gameService) =>
        {
            try
            {
                var gameId = await gameService.CreateGameAsync(request);
                var game = await gameService.GetGameByIdAsync(gameId);
                return Results.Created($"/game/{gameId}", game);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("PostGame")
        .WithTags("1. Games", "POST Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi()
        .WithDescription("Creates a new game in the database using the provided game data. Returns 201 Created with the URL of the newly created game resource in the 'Location' header and the created game in the response body.")
        .Produces(StatusCodes.Status201Created, typeof(GameDto), "application/json");


        app.MapPost("/games", async (List<CreateGameRequest> requests, IGameService gameService) =>
        {
            try
            {
                var createdGames = new List<GameDto>();

                foreach (var request in requests)
                {
                    var gameId = await gameService.CreateGameAsync(request);
                    var game = await gameService.GetGameByIdAsync(gameId);
                    createdGames.Add(game);
                }

                return Results.Created("/games", createdGames);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("PostMultipleGames")
        .WithTags("1. Games", "POST Endpoints")
        .WithOpenApi()
        .WithDescription("Creates multiple games in the database using the provided game data. Returns 201 Created with the URL of the newly created games resource in the 'Location' header and the list of created games in the response body.")
        .Produces(StatusCodes.Status201Created, typeof(List<GameDto>), "application/json");

    }
}