namespace GameScoringAPI.Services;

using GameScoringAPI.Endpoints.MatchDataPoint;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Services.Validators;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service for MatchDataPoint business logic.
/// Handles CRUD operations, validation, and data point-related queries.
/// </summary>
public class MatchDataPointService : IMatchDataPointService
{
    private readonly GameDBContext _context;
    private readonly IMatchDataPointValidator _validator;
    private readonly ILogger<MatchDataPointService> _logger;

    public MatchDataPointService(
        GameDBContext context,
        IMatchDataPointValidator validator,
        ILogger<MatchDataPointService> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IEnumerable<MatchDataPointDto>> GetAllMatchDataPointsAsync()
    {
        _logger.LogInformation("Fetching all match data points");
        var dataPoints = await _context.MatchDataPoints
            .Select(dp => new MatchDataPointDto
            {
                Id = dp.Id,
                MatchId = dp.MatchId,
                PlayerName = dp.PlayerName,
                GamePoints = dp.GamePoints,
                PointsDescription = dp.PointsDescription,
                CreatedDate = dp.CreatedDate
            })
            .ToListAsync();

        return dataPoints;
    }

    public async Task<IEnumerable<MatchDataPointDto>> GetAllMatchDataPointsDetailedAsync()
    {
        _logger.LogInformation("Fetching all match data points with detailed information");
        var dataPoints = await _context.MatchDataPoints
            .Include(dp => dp.Match)
            .ThenInclude(m => m.Game)
            .Select(dp => new MatchDataPointDto
            {
                Id = dp.Id,
                MatchId = dp.MatchId,
                PlayerName = dp.PlayerName,
                GamePoints = dp.GamePoints,
                PointsDescription = dp.PointsDescription,
                CreatedDate = dp.CreatedDate,
                isMatchFinished = dp.Match.isFinished,
                GameId = dp.Match.GameId,
                GameName = dp.Match.Game.GameName
            })
            .ToListAsync();

        return dataPoints;
    }

    public async Task<MatchDataPointDto> GetMatchDataPointByIdAsync(int id)
    {
        _logger.LogInformation("Fetching match data point {DataPointId}", id);
        var dataPoint = await _context.MatchDataPoints
            .Where(dp => dp.Id == id)
            .Select(dp => new MatchDataPointDto
            {
                Id = dp.Id,
                MatchId = dp.MatchId,
                PlayerName = dp.PlayerName,
                GamePoints = dp.GamePoints,
                PointsDescription = dp.PointsDescription,
                CreatedDate = dp.CreatedDate
            })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Match data point {id} not found");

        return dataPoint;
    }

    public async Task<IEnumerable<MatchDataPointDto>> GetDataPointsByMatchIdAsync(int matchId)
    {
        _logger.LogInformation("Fetching data points for match {MatchId}", matchId);
        var dataPoints = await _context.MatchDataPoints
            .Where(dp => dp.MatchId == matchId)
            .Select(dp => new MatchDataPointDto
            {
                Id = dp.Id,
                MatchId = dp.MatchId,
                PlayerName = dp.PlayerName,
                GamePoints = dp.GamePoints,
                PointsDescription = dp.PointsDescription,
                CreatedDate = dp.CreatedDate
            })
            .ToListAsync();

        return dataPoints;
    }

    public async Task<int> CreateMatchDataPointAsync(CreateMatchDataPointRequest request)
    {
        _logger.LogInformation("Creating match data point for match {MatchId} with player {PlayerName}", request.MatchId, request.PlayerName);

        // Validate input
        await _validator.ValidateForCreateAsync(request);

        var dataPoint = new MatchDataPoint
        {
            MatchId = request.MatchId,
            PlayerName = request.PlayerName,
            GamePoints = request.GamePoints,
            PointsDescription = request.PointsDescription,
            CreatedDate = DateTime.UtcNow
        };

        _context.MatchDataPoints.Add(dataPoint);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match data point created successfully with ID {DataPointId}", dataPoint.Id);
        return dataPoint.Id;
    }

    public async Task UpdateMatchDataPointAsync(int id, UpdateMatchDataPointRequest request)
    {
        _logger.LogInformation("Updating match data point {DataPointId}", id);

        var existingDataPoint = await _context.MatchDataPoints.FirstOrDefaultAsync(dp => dp.Id == id)
            ?? throw new NotFoundException($"Match data point {id} not found");

        // Validate input
        await _validator.ValidateForUpdateAsync(existingDataPoint, request);

        // Apply updates
        if (!string.IsNullOrWhiteSpace(request.PlayerName))
            existingDataPoint.PlayerName = request.PlayerName;

        if (request.GamePoints.HasValue)
            existingDataPoint.GamePoints = request.GamePoints.Value;

        if (!string.IsNullOrWhiteSpace(request.PointsDescription))
            existingDataPoint.PointsDescription = request.PointsDescription;

        _context.MatchDataPoints.Update(existingDataPoint);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match data point {DataPointId} updated successfully", id);
    }

    public async Task DeleteMatchDataPointAsync(int id)
    {
        _logger.LogInformation("Deleting match data point {DataPointId}", id);

        var dataPoint = await _context.MatchDataPoints.FirstOrDefaultAsync(dp => dp.Id == id)
            ?? throw new NotFoundException($"Match data point {id} not found");

        _context.MatchDataPoints.Remove(dataPoint);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match data point {DataPointId} deleted successfully", id);
    }

    public async Task<IEnumerable<DeleteMatchDataPointResult>> DeleteMultipleMatchDataPointsAsync(List<int> ids)
    {
        _logger.LogInformation("Bulk deleting {Count} match data points", ids.Count);

        var foundDataPoints = await _context.MatchDataPoints
            .Where(dp => ids.Contains(dp.Id))
            .ToListAsync();

        var foundIds = foundDataPoints.Select(dp => dp.Id).ToHashSet();
        var notFoundIds = ids.Except(foundIds);

        _context.MatchDataPoints.RemoveRange(foundDataPoints);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk deleted {Count} match data points", foundDataPoints.Count);

        var results = foundIds
            .Select(id => new DeleteMatchDataPointResult { Id = id, Deleted = true })
            .Concat(notFoundIds.Select(id => new DeleteMatchDataPointResult
            {
                Id = id,
                Deleted = false,
                Error = $"Match data point {id} not found"
            }));

        return results;
    }

    public async Task DeleteAllDataPointsForMatchAsync(int matchId)
    {
        _logger.LogInformation("Deleting all data points for match {MatchId}", matchId);

        var dataPoints = await _context.MatchDataPoints
            .Where(dp => dp.MatchId == matchId)
            .ToListAsync();

        if (dataPoints.Any())
        {
            _context.MatchDataPoints.RemoveRange(dataPoints);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted {Count} data points for match {MatchId}", dataPoints.Count, matchId);
        }
        else
        {
            _logger.LogInformation("No data points found for match {MatchId}", matchId);
        }
    }
}
