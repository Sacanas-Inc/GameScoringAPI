namespace GameScoringAPI.Services;

using GameScoringAPI.Endpoints.MatchDataPoint;

/// <summary>
/// Service interface for MatchDataPoint business logic.
/// Handles CRUD operations, validation, and data point-related queries.
/// </summary>
public interface IMatchDataPointService
{
    /// <summary>
    /// Retrieves all match data points.
    /// </summary>
    Task<IEnumerable<MatchDataPointDto>> GetAllMatchDataPointsAsync();

    /// <summary>
    /// Retrieves all match data points with detailed information (including game and match info).
    /// </summary>
    Task<IEnumerable<MatchDataPointDto>> GetAllMatchDataPointsDetailedAsync();

    /// <summary>
    /// Retrieves a match data point by ID.
    /// </summary>
    /// <throws>NotFoundException if data point doesn't exist</throws>
    Task<MatchDataPointDto> GetMatchDataPointByIdAsync(int id);

    /// <summary>
    /// Retrieves all data points for a specific match.
    /// </summary>
    Task<IEnumerable<MatchDataPointDto>> GetDataPointsByMatchIdAsync(int matchId);

    /// <summary>
    /// Creates a new match data point.
    /// </summary>
    /// <throws>ValidationException if validation fails</throws>
    /// <throws>NotFoundException if match doesn't exist</throws>
    Task<int> CreateMatchDataPointAsync(CreateMatchDataPointRequest request);

    /// <summary>
    /// Updates an existing match data point.
    /// </summary>
    /// <throws>NotFoundException if data point doesn't exist</throws>
    /// <throws>ValidationException if validation fails</throws>
    Task UpdateMatchDataPointAsync(int id, UpdateMatchDataPointRequest request);

    /// <summary>
    /// Deletes a match data point.
    /// </summary>
    /// <throws>NotFoundException if data point doesn't exist</throws>
    Task DeleteMatchDataPointAsync(int id);

    /// <summary>
    /// Deletes all data points for a specific match.
    /// </summary>
    Task DeleteAllDataPointsForMatchAsync(int matchId);

    /// <summary>
    /// Deletes multiple match data points by ID in a single transaction.
    /// Returns a result per ID indicating success or not-found.
    /// </summary>
    Task<IEnumerable<DeleteMatchDataPointResult>> DeleteMultipleMatchDataPointsAsync(List<int> ids);
}
