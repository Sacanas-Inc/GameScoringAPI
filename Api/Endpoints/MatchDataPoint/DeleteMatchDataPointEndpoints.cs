using GameScoringAPI.Endpoints.MatchDataPoint;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;

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



        app.MapDelete("/match-data-points", async ([FromBody] DeleteMatchDataPointsRequest request, IMatchDataPointService dataPointService) =>
        {
            if (request.Ids == null || request.Ids.Count == 0)
            {
                return Results.BadRequest(new { error = "No Match Data Point IDs provided." });
            }

            var results = (await dataPointService.DeleteMultipleMatchDataPointsAsync(request.Ids)).ToList();

            if (results.Any(r => !r.Deleted))
            {
                return Results.Json(results, statusCode: StatusCodes.Status207MultiStatus);
            }

            return Results.NoContent();
        })
        .WithName("DeleteMatchDataPoints")
        .WithTags("3. MatchDataPoints", "DELETE Endpoints")
        .WithOpenApi()
        .WithDescription
        (
            "Deletes multiple match data points identified by the provided IDs in a single transaction. " +
            "Returns 400 Bad Request if no IDs are provided. " +
            "Returns 204 No Content if all IDs were deleted successfully. " +
            "Returns 207 Multi-Status with a per-ID result list if any IDs were not found."
        )
        .Produces(StatusCodes.Status400BadRequest, typeof(void), "application/json")
        .Produces(StatusCodes.Status204NoContent, typeof(void), "application/json")
        .Produces(StatusCodes.Status207MultiStatus, typeof(IEnumerable<DeleteMatchDataPointResult>), "application/json");
    }
}