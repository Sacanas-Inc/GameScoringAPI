using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;


public class MatchForGameDto
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public string? Notes { get; set; }
    public bool isFinished { get; set; }
    public int PlayerCount { get; set; }
    public List<MatchDataPointForMatchDto> MatchDataPoints { get; set; } = new List<MatchDataPointForMatchDto>();
    public MatchStatsDto MatchStats { get; set; } = new MatchStatsDto();
}

public class MatchStatsDto
{
    public int TotalGamePoints { get; set; }
    public string WinningPlayer { get; set; }
    public Dictionary<string, int> PlayerPoints { get; set; } = new Dictionary<string, int>();
}

public static class GetGameEndpoints
{
    public static void MapGetGameEndpoints(this WebApplication app)
    {

        app.MapGet("/game/{id}", async (int id, IGameService gameService) =>
        {
            try
            {
                var game = await gameService.GetGameByIdAsync(id);
                return Results.Ok(game);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetGameById")
        .WithTags("1. Games", "GET Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi();


        app.MapGet("/games", async (int? id, string? description, IGameService gameService) =>
        {
            try
            {
                var games = await gameService.GetAllGamesAsync();

                // Filter by ID if provided
                if (id.HasValue)
                {
                    games = games.Where(g => g.Id == id.Value).ToList();
                    if (!games.Any())
                    {
                        return Results.NotFound(new { error = "No games found with provided params." });
                    }
                }

                // Filter by description if provided
                if (!string.IsNullOrWhiteSpace(description))
                {
                    games = games.Where(g => g.GameDescription != null && g.GameDescription.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (!games.Any())
                    {
                        return Results.Ok(new List<GameDto>()); // Return empty list with 200 OK
                    }
                }

                // If no filters provided and no games exist
                if (!games.Any())
                {
                    return Results.NotFound(new { error = "No games found." });
                }

                return Results.Ok(games);
            }
            catch (ServiceException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetAllGames")
        .WithTags("1. Games", "GET Endpoints", "9. FrontEnd - Mockup")
        .WithOpenApi();
        

        app.MapGet("/games-with-matches-and-data-points", async (string? gameName, DateTime? matchDateAfter, DateTime? matchDateBefore, GameDBContext context) =>
        {
            IQueryable<Game> gamesQuery = context.Games.Include(g => g.Matches).ThenInclude(m => m.MatchDataPoints);
            //IQueryable<Game> gamesQuery = context.Games;

            if (!string.IsNullOrEmpty(gameName))
                gamesQuery = gamesQuery.Where(g => g.GameName.Contains(gameName));
            if (matchDateAfter.HasValue)
                gamesQuery = gamesQuery.Where(g => g.Matches.Any(m => m.MatchDate >= matchDateAfter));
            if (matchDateBefore.HasValue)
                gamesQuery = gamesQuery.Where(g => g.Matches.Any(m => m.MatchDate <= matchDateBefore));

            List<Game> games = await gamesQuery.ToListAsync();

            List<GameWithMatchDataPointDto> gamesWithMatchesDto = games.Select(game => new GameWithMatchDataPointDto
            {
                Id = game.Id,
                GameName = game.GameName,
                GameDescription = game.GameDescription,
                MinPlayers = game.MinPlayers,
                MaxPlayers = game.MaxPlayers,
                AverageDuration = game.AverageDuration,
                MatchesCount = game.MatchesCount,
                // TODO: We are starting to repeate this code. Check GetMatchEndoint.cs
                Matches = game.Matches.Select(match => new MatchForGameDto
                {
                    MatchId = match.Id,
                    MatchDate = match.MatchDate,
                    Notes = match.Notes,
                    isFinished = match.isFinished,
                    PlayerCount = match.PlayerCount,
                    MatchDataPoints = match.MatchDataPoints.Select(dp => new MatchDataPointForMatchDto
                    {
                        Id = dp.Id,
                        PlayerName = dp.PlayerName,
                        GamePoints = dp.GamePoints,
                        PointsDescription = dp.PointsDescription,
                        CreatedDate = dp.CreatedDate
                    }).ToList(),
                    MatchStats = new MatchStatsDto
                    {
                        TotalGamePoints = match.MatchDataPoints.Sum(dp => dp.GamePoints),
                        PlayerPoints = match.MatchDataPoints
                            .GroupBy(dp => dp.PlayerName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Sum(dp => dp.GamePoints)
                            )
                    }
                }).ToList()
            }).ToList();

            // TODO: This bit is definitely repeated.
            // Now let's calculate the winning player for each game
            foreach (var game in gamesWithMatchesDto)
            {
                foreach (var match in game.Matches)
                {   
                    if(match.MatchStats.PlayerPoints.Any())
                    {
                        // Find the player with the maximum points
                        var winningPlayer = match.MatchStats.PlayerPoints.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                        // Set the winning player in MatchStatsDto
                        match.MatchStats.WinningPlayer = winningPlayer;
                    }
                    else
                    {
                        // No players found, set WinningPlayer to null or some default value
                        match.MatchStats.WinningPlayer = null; // or any default value you prefer
                    }
                }
            }

            return Results.Ok(gamesWithMatchesDto);
        })
        .WithName("GetGamesWithMatchesAndDataPoints")
        .WithTags("0. Full Dataset", "GET Endpoints")
        .WithOpenApi();
    
    }
}