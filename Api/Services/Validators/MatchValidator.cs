namespace GameScoringAPI.Services.Validators;

using GameScoringAPI.Endpoints.Match;
using GameScoringAPI.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Validator for Match business logic.
/// Handles field-level and business-level validation.
/// </summary>
public class MatchValidator : IMatchValidator
{
    private readonly GameDBContext _context;
    private readonly IGameService _gameService;
    private readonly ILogger<MatchValidator> _logger;

    public MatchValidator(GameDBContext context, IGameService gameService, ILogger<MatchValidator> logger)
    {
        _context = context;
        _gameService = gameService;
        _logger = logger;
    }

    public async Task ValidateForCreateAsync(CreateMatchRequest request)
    {
        _logger.LogInformation("Validating match creation request");
        var errors = new List<string>();

        // Field-level validation
        if (request.GameId <= 0)
            errors.Add("GameId must be greater than 0.");

        if (request.MatchDate == default)
            errors.Add("MatchDate is required.");

        // Business-level validation: GameId must exist
        try
        {
            await _gameService.GetGameByIdAsync(request.GameId);
        }
        catch (NotFoundException)
        {
            errors.Add($"Game with ID {request.GameId} not found.");
        }

        if (errors.Any())
            throw new ValidationException(errors);
    }

    public async Task ValidateForUpdateAsync(Match existingMatch, UpdateMatchRequest request)
    {
        _logger.LogInformation("Validating match update request for match {MatchId}", existingMatch.Id);
        var errors = new List<string>();

        // Field-level validation
        if (request.GameId <= 0)
            errors.Add("GameId must be greater than 0.");

        if (request.MatchDate == default)
            errors.Add("MatchDate is required.");

        // Business-level validation: GameId must exist
        try
        {
            await _gameService.GetGameByIdAsync(request.GameId);
        }
        catch (NotFoundException)
        {
            errors.Add($"Game with ID {request.GameId} not found.");
        }

        // Prevent GameId change if match has data points
        if (existingMatch.GameId != request.GameId)
        {
            var hasDataPoints = await _context.MatchDataPoints.AnyAsync(m => m.MatchId == existingMatch.Id);
            if (hasDataPoints)
                errors.Add("Cannot change GameId when match has data points.");
        }

        if (errors.Any())
            throw new ValidationException(errors);
    }
}
