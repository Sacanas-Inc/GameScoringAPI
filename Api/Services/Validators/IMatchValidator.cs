namespace GameScoringAPI.Services.Validators;

using GameScoringAPI.Endpoints.Match;

public interface IMatchValidator
{
    Task ValidateForCreateAsync(CreateMatchRequest request);
    Task ValidateForUpdateAsync(Match existingMatch, UpdateMatchRequest request);
}
