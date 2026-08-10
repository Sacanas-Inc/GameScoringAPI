namespace GameScoringAPI.Endpoints.MatchDataPoint;

public class CreateMatchDataPointRequest
{
    public int MatchId { get; set; }
    public string PlayerName { get; set; }
    public int GamePoints { get; set; }
    public string PointsDescription { get; set; }
}

public class UpdateMatchDataPointRequest
{
    public int? MatchId { get; set; }
    public string? PlayerName { get; set; }
    public int? GamePoints { get; set; }
    public string? PointsDescription { get; set; }
}
