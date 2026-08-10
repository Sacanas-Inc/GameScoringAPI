namespace GameScoringAPI.Services.Validators;

/// <summary>
/// Validates Game entity for create and update operations.
/// </summary>
public interface IGameValidator
{
    /// <summary>
    /// Validates a game for creation (field-level and business-level checks).
    /// </summary>
    Task ValidateForCreateAsync(CreateGameRequest request);

    /// <summary>
    /// Validates a game update (field-level and business-level checks).
    /// </summary>
    Task ValidateForUpdateAsync(Game existingGame, UpdateGameRequest request);
}
