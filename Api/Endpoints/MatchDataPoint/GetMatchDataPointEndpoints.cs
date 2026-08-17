using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;

public static class GetMatchDataPointEndpoints
{
    public static void MapGetMatchDataPointEndpoints(this WebApplication app)
    {        
    
        app.MapGet("/match-data-points/all/detailed", async (IMatchDataPointService dataPointService) =>
        {
            try
            {
                var matchDataPoints = await dataPointService.GetAllMatchDataPointsDetailedAsync();
                return Results.Ok(matchDataPoints);
            }
            catch (Exception ex)
            {
                return Results.InternalServerError();
            }
        })
        .WithName("GetDetailedMatchDataPoints")
        .WithTags("3. MatchDataPoints", "GET Endpoints")
        .WithOpenApi();


        app.MapGet("/match-data-points/all", async (IMatchDataPointService dataPointService) =>
        {
            try
            {
                var matchDataPoints = await dataPointService.GetAllMatchDataPointsAsync();
                return Results.Ok(matchDataPoints);
            }
            catch (Exception ex)
            {
                return Results.InternalServerError();
            }
        })
        .WithName("GetMatchDataPoints")
        .WithTags("3. MatchDataPoints", "GET Endpoints")
        .WithOpenApi();

        app.MapGet("/match-data-point/{id}", async (int id, IMatchDataPointService dataPointService) =>
        {
            try
            {
                var dataPoint = await dataPointService.GetMatchDataPointByIdAsync(id);
                return Results.Ok(dataPoint);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("GetMatchDataPointById")
        .WithTags("3. MatchDataPoints", "GET Endpoints")
        .WithOpenApi();

        app.MapGet("/match/{matchId}/data-points", async (int matchId, IMatchDataPointService dataPointService) =>
        {
            try
            {
                var dataPoints = await dataPointService.GetDataPointsByMatchIdAsync(matchId);
                return Results.Ok(dataPoints);
            }
            catch (Exception ex)
            {
                return Results.InternalServerError();
            }
        })
        .WithName("GetDataPointsByMatchId")
        .WithTags("3. MatchDataPoints", "GET Endpoints")
        .WithOpenApi();

    }
}