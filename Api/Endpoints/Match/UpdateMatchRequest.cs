namespace GameScoringAPI.Endpoints.Match;

public class UpdateMatchRequest
{
    public int GameId { get; set; }
    public DateTime MatchDate { get; set; }
    public string? Notes { get; set; }
    public bool isFinished { get; set; }
}
