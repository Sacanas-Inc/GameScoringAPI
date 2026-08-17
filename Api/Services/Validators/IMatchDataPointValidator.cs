namespace GameScoringAPI.Services.Validators;

using GameScoringAPI.Endpoints.MatchDataPoint;

public interface IMatchDataPointValidator
{
    Task ValidateForCreateAsync(CreateMatchDataPointRequest request);
    Task ValidateForUpdateAsync(MatchDataPoint existingDataPoint, UpdateMatchDataPointRequest request);
}
