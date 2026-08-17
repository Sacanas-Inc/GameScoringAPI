namespace GameScoringAPI.Services;

using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Services.Validators;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service implementation for Game business logic.
/// Handles CRUD operations with validation and error handling.
/// </summary>
public class GameService : IGameService
{
    private readonly GameDBContext _context;
    private readonly IGameValidator _validator;
    private readonly ILogger<GameService> _logger;

    public GameService(GameDBContext context, IGameValidator validator, ILogger<GameService> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IEnumerable<GameDto>> GetAllGamesAsync()
    {
        _logger.LogInformation("Fetching all games");
        var games = await _context.Games.ToListAsync();
        return games.Select(g => MapToDto(g)).ToList();
    }

    public async Task<GameDto> GetGameByIdAsync(int id)
    {
        _logger.LogInformation("Fetching game {GameId}", id);
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new NotFoundException($"Game with ID {id} not found.");
        return MapToDto(game);
    }

    public async Task<int> CreateGameAsync(CreateGameRequest request)
    {
        _logger.LogInformation("Creating new game: {GameName}", request.GameName);

        // Validate request
        await _validator.ValidateForCreateAsync(request);

        var game = new Game
        {
            GameName = request.GameName,
            GameDescription = request.GameDescription,
            MinPlayers = request.MinPlayers,
            MaxPlayers = request.MaxPlayers,
            AverageDuration = request.AverageDuration,
            MatchesCount = 0
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game created successfully with ID {GameId}", game.Id);
        return game.Id;
    }

    public async Task UpdateGameAsync(int id, UpdateGameRequest request)
    {
        _logger.LogInformation("Updating game {GameId}", id);

        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new NotFoundException($"Game with ID {id} not found.");

        // Validate request
        await _validator.ValidateForUpdateAsync(game, request);

        game.GameName = request.GameName;
        game.GameDescription = request.GameDescription;
        game.MinPlayers = request.MinPlayers;
        game.MaxPlayers = request.MaxPlayers;
        game.AverageDuration = request.AverageDuration;

        _context.Games.Update(game);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game {GameId} updated successfully", id);
    }

    public async Task DeleteGameAsync(int id)
    {
        _logger.LogInformation("Deleting game {GameId}", id);

        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new NotFoundException($"Game with ID {id} not found.");

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game {GameId} deleted successfully", id);
    }

    /// <summary>
    /// Maps a Game entity to a GameDto.
    /// </summary>
    private static GameDto MapToDto(Game game)
    {
        return new GameDto
        {
            Id = game.Id,
            GameName = game.GameName,
            GameDescription = game.GameDescription,
            MinPlayers = game.MinPlayers,
            MaxPlayers = game.MaxPlayers,
            AverageDuration = game.AverageDuration,
            MatchesCount = game.MatchesCount
        };
    }
}
