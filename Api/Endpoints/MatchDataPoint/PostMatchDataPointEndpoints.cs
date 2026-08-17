using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Endpoints.MatchDataPoint;

public class ReturnPostMatchDataPointDto
{
    public int id { get; set; }
    public int MatchId { get; set; }
    public string PlayerName { get; set; }
    public int GamePoints { get; set; }
    public string PointsDescription { get; set; }
    public DateTime CreatedDate { get; set; }
}

public static class PostMatchDataPointEndpoints
{
    public static void MapPostMatchDataPointEndpoints(this WebApplication app)
    {   

        app.MapPost("/match-data-point/{MatchId}", async (int matchId, CreateMatchDataPointRequest request, IMatchDataPointService dataPointService) =>
        {
            try
            {
                var dataPointId = await dataPointService.CreateMatchDataPointAsync(new CreateMatchDataPointRequest
                {
                    MatchId = matchId,
                    PlayerName = request.PlayerName,
                    GamePoints = request.GamePoints,
                    PointsDescription = request.PointsDescription
                });

                var createdDataPoint = await dataPointService.GetMatchDataPointByIdAsync(dataPointId);

                var returnDto = new ReturnPostMatchDataPointDto
                {
                    id = createdDataPoint.Id,
                    MatchId = createdDataPoint.MatchId,
                    PlayerName = createdDataPoint.PlayerName,
                    GamePoints = createdDataPoint.GamePoints,
                    PointsDescription = createdDataPoint.PointsDescription,
                    CreatedDate = createdDataPoint.CreatedDate
                };

                return Results.Created($"/match-data-points/{dataPointId}", returnDto);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("PostMatchDataPoint")
        .WithTags("3. MatchDataPoints", "POST Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi()
        .WithDescription("Creates a new match data point for the specified match in the database using the provided data. Returns 201 Created with the URL of the newly created match data point resource in the 'Location' header and the created match data point in the response body.")
        .Produces(StatusCodes.Status201Created, typeof(ReturnPostMatchDataPointDto), "application/json");
    }
}