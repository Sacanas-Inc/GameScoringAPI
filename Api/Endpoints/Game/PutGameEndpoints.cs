using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class PutGameEndpoints
{
    public static void MapPutGameEndpoints(this WebApplication app)
    {
        app.MapPut("/game/{id}", async (int id, UpdateGameRequest request, IGameService gameService) =>
        {
            try
            {
                await gameService.UpdateGameAsync(id, request);
                var game = await gameService.GetGameByIdAsync(id);
                return Results.Ok(game);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
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
        .WithName("PutGame")
        .WithTags("1. Games", "PUT Endpoints")
        .WithOpenApi();
    

        app.MapPut("/games", async (List<UpdateGameRequest> requests, IGameService gameService) =>
        {
            try
            {
                // Note: This endpoint needs game IDs. Current design doesn't support bulk updates well.
                // Consider refactoring to accept a list of (id, UpdateGameRequest) pairs.
                return Results.BadRequest(new { error = "Bulk update endpoint needs redesign - use individual PUT /game/{id} instead." });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("PutMultipleGames")
        .WithTags("1. Games", "PUT Endpoints")
        .WithOpenApi();
        
    }
}