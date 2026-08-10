using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class DeleteMatchDataPointEndpoints
{
    public static void MapDeleteMatchDataPointEndpoints(this WebApplication app)
    {
        app.MapDelete("/match-data-point/{id}", async (int id, IMatchDataPointService dataPointService) =>
        {
            try
            {
                await dataPointService.DeleteMatchDataPointAsync(id);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteMatchDataPoint")
        .WithTags("3. MatchDataPoints", "DELETE Endpoints")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes a single match data point from the database identified by the provided ID. Returns 404 Not Found if the match data point with the specified ID is not found. Upon successful deletion, returns 204 No Content."
        )
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json");



        app.MapDelete("/match-data-points", async (IMatchDataPointService dataPointService, params int[] ids) =>
        {
            if (ids == null || ids.Length == 0)
            {
                return Results.BadRequest("No Match Data Point IDs provided.");
            }

            try
            {
                foreach (var id in ids)
                {
                    await dataPointService.DeleteMatchDataPointAsync(id);
                }
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteMatchDataPoints")
        .WithTags("3. MatchDataPoints", "DELETE Endpoints")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes multiple match data points from the database identified by the provided IDs. Returns 400 Bad Request if no match data point IDs are provided. Returns 404 Not Found if any of the specified IDs are not found. Upon successful deletion, returns 204 No Content."
        )
        .Produces(StatusCodes.Status400BadRequest, typeof(void), "application/json")
        .Produces(StatusCodes.Status404NotFound, typeof(string), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json");
    }
}