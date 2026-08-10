using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class DeleteGameEndpoints
{
    public static void MapDeleteGameEndpoints(this WebApplication app)
    {

        app.MapDelete("/game/{id}", async (int id, IGameService gameService) =>
        {
            try
            {
                await gameService.DeleteGameAsync(id);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("DeleteGame")
        .WithTags("1. Games", "DELETE Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes a game from the database identified by the provided ID. Returns 404 Not Found if the game with the specified ID is not found. Upon successful deletion, returns 204 No Content."
        )
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json")
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json");




        app.MapDelete("/games", async (IGameService gameService, params int[] gameIds) =>
        {
            try
            {
                // Check if gameIds is null or empty
                if (gameIds == null || gameIds.Length == 0)
                {
                    return Results.BadRequest(new { error = "No game IDs provided." });
                }

                // Iterate through each gameId and delete
                foreach (var id in gameIds)
                {
                    await gameService.DeleteGameAsync(id);
                }

                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("DeleteMultipleGames")
        .WithTags("1. Games", "DELETE Endpoints")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes multiple games from the database identified by the provided IDs. Returns 400 Bad Request if no game IDs are provided. Returns 404 Not Found if any of the specified game IDs are not found, none of the objects are deleted when this happens. Upon successful deletion, returns 204 No Content."
        )
        .Produces(StatusCodes.Status400BadRequest, typeof(void), "application/json")
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json"); 
        
    }
}