using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class DeleteMatchEndpoints
{
    public static void MapDeleteMatchEndpoints(this WebApplication app)
    {
        app.MapDelete("/match/{id}", async (int id, IMatchService matchService) =>
        {
            try
            {
                await matchService.DeleteMatchAsync(id);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteMatch")
        .WithTags("2. Matches", "DELETE Endpoints")
        .WithOpenApi()
        .WithDescription("Deletes a match and all associated data points (via DB cascade) identified by the provided ID. Returns 204 No Content on success, 404 Not Found if the match does not exist.")
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json");

        app.MapDelete("/match-and-data-points/{id}", async (int id, IMatchService matchService, IMatchDataPointService dataPointService) =>
        {
            try
            {
                // Delete all MatchDataPoints associated with this match
                await dataPointService.DeleteAllDataPointsForMatchAsync(id);

                // Delete the match
                await matchService.DeleteMatchAsync(id);

                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteMatchAndAllDataPoints")
        .WithTags("2. Matches", "DELETE Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes a match and all associated data points from the database identified by the provided ID. Returns 404 Not Found if the match with the specified ID is not found. Upon successful deletion, returns 204 No Content."
        )
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json");
    }
}
