namespace GameScoringAPI.Services.Validators;

using GameScoringAPI.Endpoints.MatchDataPoint;
using GameScoringAPI.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Validator for MatchDataPoint business logic.
/// Handles field-level and business-level validation.
/// </summary>
public class MatchDataPointValidator : IMatchDataPointValidator
{
    private readonly GameDBContext _context;
    private readonly IMatchService _matchService;
    private readonly ILogger<MatchDataPointValidator> _logger;

    public MatchDataPointValidator(GameDBContext context, IMatchService matchService, ILogger<MatchDataPointValidator> logger)
    {
        _context = context;
        _matchService = matchService;
        _logger = logger;
    }

    public async Task ValidateForCreateAsync(CreateMatchDataPointRequest request)
    {
        _logger.LogInformation("Validating match data point creation request for match {MatchId}", request.MatchId);
        var errors = new List<string>();

        // Field-level validation
        if (request.MatchId <= 0)
            errors.Add("MatchId must be greater than 0.");

        if (string.IsNullOrWhiteSpace(request.PlayerName))
            errors.Add("PlayerName is required.");

        if (request.GamePoints < 0)
            errors.Add("GamePoints cannot be negative.");

        if (string.IsNullOrWhiteSpace(request.PointsDescription))
            errors.Add("PointsDescription is required.");

        // Business-level validation: MatchId must exist
        try
        {
            await _matchService.GetMatchByIdAsync(request.MatchId);
        }
        catch (NotFoundException)
        {
            errors.Add($"Match with ID {request.MatchId} not found.");
        }

        if (errors.Any())
            throw new ValidationException(errors);
    }

    public async Task ValidateForUpdateAsync(MatchDataPoint existingDataPoint, UpdateMatchDataPointRequest request)
    {
        _logger.LogInformation("Validating match data point update for data point {DataPointId}", existingDataPoint.Id);
        var errors = new List<string>();

        // Field-level validation
        if (!string.IsNullOrWhiteSpace(request.PlayerName) && string.IsNullOrWhiteSpace(request.PlayerName))
            errors.Add("PlayerName cannot be empty if provided.");

        if (request.GamePoints.HasValue && request.GamePoints < 0)
            errors.Add("GamePoints cannot be negative.");

        if (!string.IsNullOrWhiteSpace(request.PointsDescription) && string.IsNullOrWhiteSpace(request.PointsDescription))
            errors.Add("PointsDescription cannot be empty if provided.");

        // Business-level validation: MatchId cannot be changed
        if (request.MatchId.HasValue && request.MatchId != existingDataPoint.MatchId)
            errors.Add("Cannot change MatchId of an existing data point.");

        if (errors.Any())
            throw new ValidationException(errors);
    }
}
