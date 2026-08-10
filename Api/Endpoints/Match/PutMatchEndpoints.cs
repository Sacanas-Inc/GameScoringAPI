using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Endpoints.Match;

public class PutSingleMatchDto
{
    public int GameId { get; set; }
    public DateTime MatchDate { get; set; }
    public string Notes { get; set; }
    public bool isFinished { get; set; }
}


public static class PutMatchEndpoints
{
    public static void MapPutMatchEndpoints(this WebApplication app)
    {      
        app.MapPut("/match/{id}", async (int id, UpdateMatchRequest request, IMatchService matchService) =>
        {
            try
            {
                await matchService.UpdateMatchAsync(id, request);
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
        .WithName("PutMatch")
        .WithTags("2. Matches", "PUT Endpoints")
        .WithOpenApi();    
    }
}
