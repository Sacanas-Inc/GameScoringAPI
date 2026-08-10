using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Endpoints.Match;

// Declared DTO here because I only intend on using it here.
public class PostSingleMatchDto
{
    public int GameId { get; set; }
    public DateTime MatchDate { get; set; }
    public string Notes { get; set; }
    public bool isFinished { get; set; }
}


public static class PostMatchEndpoints
{
    public static void MapPostMatchEndpoints(this WebApplication app)
    {    
        app.MapPost("/match", async (CreateMatchRequest request, IMatchService matchService) =>
        {
            try
            {
                var matchId = await matchService.CreateMatchAsync(request);
                var match = await matchService.GetMatchByIdAsync(matchId);
                return Results.Created($"/match/{matchId}", match);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { errors = ex.Errors });
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("PostMatch")
        .WithTags("2. Matches", "POST Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi()
        .WithDescription("Creates a new match in the database using the provided match data. Returns 201 Created with the URL of the newly created match resource in the 'Location' header and the created match in the response body.")
        .Produces(StatusCodes.Status201Created, typeof(MatchForMatchDto), "application/json");
    
    }
}

