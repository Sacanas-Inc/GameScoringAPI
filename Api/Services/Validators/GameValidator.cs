namespace GameScoringAPI.Services.Validators;

using GameScoringAPI.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of game validation logic.
/// Performs field-level validation (required fields, ranges) and business-level validation (duplicates, constraints).
/// </summary>
public class GameValidator : IGameValidator
{
    private readonly GameDBContext _context;

    public GameValidator(GameDBContext context)
    {
        _context = context;
    }

    public async Task ValidateForCreateAsync(CreateGameRequest request)
    {
        var errors = new List<string>();

        // Field-level validation
        if (string.IsNullOrWhiteSpace(request.GameName))
            errors.Add("GameName cannot be empty.");

        if (request.MinPlayers < 0)
            errors.Add("MinPlayers cannot be negative.");

        if (request.MaxPlayers < 0)
            errors.Add("MaxPlayers cannot be negative.");

        if (request.MaxPlayers > 0 && request.MinPlayers > request.MaxPlayers)
            errors.Add("MinPlayers cannot be greater than MaxPlayers.");

        if (request.AverageDuration < 0)
            errors.Add("AverageDuration cannot be negative.");

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Business-level validation
        var existingGame = await _context.Games
            .FirstOrDefaultAsync(g => g.GameName == request.GameName);

        if (existingGame != null)
            throw new ValidationException($"A game with name '{request.GameName}' already exists.");
    }

    public async Task ValidateForUpdateAsync(Game existingGame, UpdateGameRequest request)
    {
        var errors = new List<string>();

        // Field-level validation
        if (string.IsNullOrWhiteSpace(request.GameName))
            errors.Add("GameName cannot be empty.");

        if (request.MinPlayers < 0)
            errors.Add("MinPlayers cannot be negative.");

        if (request.MaxPlayers < 0)
            errors.Add("MaxPlayers cannot be negative.");

        if (request.MaxPlayers > 0 && request.MinPlayers > request.MaxPlayers)
            errors.Add("MinPlayers cannot be greater than MaxPlayers.");

        if (request.AverageDuration < 0)
            errors.Add("AverageDuration cannot be negative.");

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Business-level validation: check for duplicate name (excluding current game)
        var existingWithSameName = await _context.Games
            .FirstOrDefaultAsync(g => g.GameName == request.GameName && g.Id != existingGame.Id);

        if (existingWithSameName != null)
            throw new ValidationException($"A game with name '{request.GameName}' already exists.");
    }
}
