using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Endpoints.MatchDataPoint;

public static class PutMatchDataPointEndpoints
{
    public static void MapPutMatchDataPointEndpoints(this WebApplication app)
    {        
        app.MapPut("/match-data-point/{id}", async (int id, UpdateMatchDataPointRequest request, IMatchDataPointService dataPointService) =>
        {
            try
            {
                await dataPointService.UpdateMatchDataPointAsync(id, request);
                return Results.NoContent();
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return Results.Conflict(new { errors = ex.Errors });
            }
        })
        .WithName("PutMatchDataPoint")
        .WithTags("3. MatchDataPoints", "PUT Endpoints")
        .WithOpenApi();
    }
}