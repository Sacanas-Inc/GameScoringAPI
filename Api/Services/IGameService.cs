namespace GameScoringAPI.Services;

/// <summary>
/// Service interface for Game business logic.
/// Handles CRUD operations, validation, and game-related queries.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Retrieves all games.
    /// </summary>
    Task<IEnumerable<GameDto>> GetAllGamesAsync();

    /// <summary>
    /// Retrieves a game by ID.
    /// </summary>
    /// <throws>NotFoundException if game doesn't exist</throws>
    Task<GameDto> GetGameByIdAsync(int id);

    /// <summary>
    /// Creates a new game.
    /// </summary>
    /// <returns>The ID of the created game.</returns>
    /// <throws>ValidationException if validation fails</throws>
    Task<int> CreateGameAsync(CreateGameRequest request);

    /// <summary>
    /// Updates an existing game.
    /// </summary>
    /// <throws>NotFoundException if game doesn't exist</throws>
    /// <throws>ValidationException if validation fails</throws>
    Task UpdateGameAsync(int id, UpdateGameRequest request);

    /// <summary>
    /// Deletes a game.
    /// </summary>
    /// <throws>NotFoundException if game doesn't exist</throws>
    Task DeleteGameAsync(int id);
}
