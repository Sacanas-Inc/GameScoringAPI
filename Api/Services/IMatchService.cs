namespace GameScoringAPI.Services;

using GameScoringAPI.Endpoints.Match;

public interface IMatchService
{
    Task<IEnumerable<MatchForMatchDto>> GetAllMatchesAsync();
    Task<MatchForMatchDto> GetMatchByIdAsync(int id, bool includeDataPoints = false);
    Task<MatchForMatchDto> GetMatchWithWinnerAsync(int id);
    Task<int> CreateMatchAsync(CreateMatchRequest request);
    Task UpdateMatchAsync(int id, UpdateMatchRequest request);
    Task DeleteMatchAsync(int id);
}
