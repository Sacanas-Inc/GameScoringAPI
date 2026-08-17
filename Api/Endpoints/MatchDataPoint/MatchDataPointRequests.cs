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

public class DeleteMatchDataPointsRequest
{
    public List<int> Ids { get; set; } = [];
}

public class DeleteMatchDataPointResult
{
    public int Id { get; set; }
    public bool Deleted { get; set; }
    public string? Error { get; set; }
}
