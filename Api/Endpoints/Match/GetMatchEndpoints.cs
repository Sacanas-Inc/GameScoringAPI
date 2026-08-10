using GameScoringAPI.Mapper;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

public class MatchForMatchDto
{
    public int MatchId { get; set; }
    public int GameId { get; set; }
    public DateTime MatchDate { get; set; }
    public string? Notes { get; set; }
    public int PlayerCount { get; set; }
    public bool isFinished { get; set; }
    public List<MatchDataPointForMatchDto>? MatchDataPoints { get; set; } = new List<MatchDataPointForMatchDto>();
    public MatchStatsDto? MatchStats { get; set; } = new MatchStatsDto();

}

public class MatchDataPointForMatchDto
{
    public int Id { get; set; }
    public string PlayerName { get; set; }
    public int GamePoints { get; set; }
    public string PointsDescription { get; set; }
    public DateTime CreatedDate { get; set; }
}


public static class GetMatchEndpoints
{
    public static void MapGetMatchEndpoints(this WebApplication app)
    {    
        app.MapGet("/match/{id}", async (int id, bool? includeDataPoints, IMatchService matchService) =>
        {
            try
            {
                var match = await matchService.GetMatchByIdAsync(id, includeDataPoints ?? false);
                return Results.Ok(match);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("GetMatch")
        .WithTags("2. Matches", "GET Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi(); 


        app.MapGet("/matches", async (bool? includeDataPoints, int? gameId, IMatchService matchService) =>
        {
            try
            {
                var matches = await matchService.GetAllMatchesAsync();
                
                // Apply filter if gameId is provided
                if (gameId.HasValue)
                    matches = matches.Where(m => m.GameId == gameId.Value);

                var matchList = matches.ToList();

                // Include stats and calculate winner if necessary
                if (includeDataPoints is true)
                {
                    foreach (var match in matchList)
                    {
                        if (match.MatchDataPoints?.Count > 0)
                        {
                            match.MatchStats = new MatchMapper().CreateMathStatsFor(match);
                            new MatchMapper().CalculateWinnerFor(match);
                        }
                    }
                }

                return Results.Ok(matchList);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        })
        .WithName("GetAllMatches")
        .WithTags("2. Matches", "GET Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi();
           
    }
}