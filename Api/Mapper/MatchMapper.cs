
namespace GameScoringAPI.Mapper;


/// <summary>
/// CLass to agregate Mapping of Match Entities to severel Match related DTOs. 
/// Query Objects should be provided already filtered, and should be resolved after the output of these methods.
/// </summary>
public class MatchMapper
{

    /// <summary>
    /// Method used the .Select() method of the IQueryable class to map the Match model to the MatchDto, 
    /// if we provide the includeDataPoints as true, it also resolves and maps the MatchDataPoint
    /// entity to the MatchDataPointforMatchDto.
    /// </summary>
    /// <param name="match">The IQueryable<Match> of the database model, to be selected into thhe DTO.</param>
    /// <param name="includeDataPoints"> If we shouuld include the child objects includeDataPoints when doing the Select.</param>
    /// <returns></returns>
    public IQueryable<MatchForMatchDto> SelectAndMapToDTO(IQueryable<Match>? match, bool? includeDataPoints)
    {   
          return match
                .Select(m => new MatchForMatchDto
                {
                    MatchId = m.Id,
                    GameId = m.GameId,
                    MatchDate = m.MatchDate,
                    Notes = m.Notes,
                    isFinished = m.isFinished,
                    PlayerCount = m.PlayerCount,
                    MatchDataPoints = includeDataPoints == true
                        ? m.MatchDataPoints.Select(dp => new MatchDataPointForMatchDto
                        {
                            Id = dp.Id,
                            PlayerName = dp.PlayerName,
                            GamePoints = dp.GamePoints,
                            PointsDescription = dp.PointsDescription,
                            CreatedDate = dp.CreatedDate
                        }).ToList()
                        : new List<MatchDataPointForMatchDto>()
                });
    }


    public MatchStatsDto CreateMathStatsFor(MatchForMatchDto matchDto)
    {
        return new MatchStatsDto
            {
                TotalGamePoints = matchDto.MatchDataPoints.Sum(dp => dp.GamePoints),
                PlayerPoints = matchDto.MatchDataPoints
                    .GroupBy(dp => dp.PlayerName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(dp => dp.GamePoints)
                    )
            };
    }

    /// <summary>
    /// Modifies a Match after calculating the winning player of the Match, calculations are based on the MatchStats of the Match
    /// </summary>
    /// <param name="matchDto"></param>
    public void CalculateWinnerFor(MatchForMatchDto matchDto)
    {
         // Check if PlayerPoints dictionary is not empty
        if (!matchDto.MatchStats.PlayerPoints.Any())
        {
            matchDto.MatchStats.WinningPlayer = null;
            return;
        }
        // Find the player with the maximum points  // Set the winning player in MatchStatsDto
        string winningPlayer = matchDto.MatchStats.PlayerPoints
            .Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        matchDto.MatchStats.WinningPlayer = winningPlayer;
    }



        /*
            public static MatchDetailsDTO MapToDetailsDTO(Match match, Game game, List<MatchDataPoint> matchDataPoints)
            {
                return new MatchDetailsDTO
                {
                    MatchId = match.Id,
                    GameId = match.GameId,
                    MatchDate = match.MatchDate,
                    Notes = match.Notes,
                    PlayerCount = match.PlayerCount,
                    IsFinished = match.IsFinished,
                    Game = new GameDTO
                    {
                        GameId = game.Id,
                        Name = game.Name
                    },
                    MatchDataPoints = matchDataPoints.Select(dp => new MatchDataPointDTO
                    {
                        DataPointId = dp.Id,
                        MatchId = dp.MatchId,
                        PlayerName = dp.PlayerName,
                        GamePoints = dp.GamePoints
                    }).ToList()
                };
            }
        */
    }