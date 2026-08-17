namespace GameScoringAPI.Services;

using GameScoringAPI.Endpoints.Match;
using GameScoringAPI.Mapper;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Services.Validators;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service implementation for Match business logic.
/// Handles CRUD operations with validation and error handling.
/// </summary>
public class MatchService : IMatchService
{
    private readonly GameDBContext _context;
    private readonly IMatchValidator _validator;
    private readonly MatchMapper _mapper;
    private readonly ILogger<MatchService> _logger;

    public MatchService(GameDBContext context, IMatchValidator validator, MatchMapper mapper, ILogger<MatchService> logger)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<MatchForMatchDto>> GetAllMatchesAsync()
    {
        _logger.LogInformation("Fetching all matches");
        var matches = await _context.Matches
            .Include(m => m.MatchDataPoints)
            .ToListAsync();

        return matches.Select(m => MapToDto(m));
    }

    public async Task<MatchForMatchDto> GetMatchByIdAsync(int id, bool includeDataPoints = false)
    {
        _logger.LogInformation("Fetching match {MatchId}, includeDataPoints: {IncludeDataPoints}", id, includeDataPoints);

        var query = _context.Matches.AsQueryable();
        if (includeDataPoints)
            query = query.Include(m => m.MatchDataPoints);

        var match = await query.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Match with ID {id} not found.");

        var dto = MapToDto(match);

        // Add stats if data points included
        if (includeDataPoints && match.MatchDataPoints?.Count > 0)
        {
            dto.MatchStats = _mapper.CreateMathStatsFor(dto);
        }

        return dto;
    }

    public async Task<MatchForMatchDto> GetMatchWithWinnerAsync(int id)
    {
        _logger.LogInformation("Fetching match {MatchId} with winner calculation", id);

        var match = await _context.Matches
            .Include(m => m.MatchDataPoints)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Match with ID {id} not found.");

        var dto = MapToDto(match);
        
        if (match.MatchDataPoints?.Count > 0)
        {
            dto.MatchStats = _mapper.CreateMathStatsFor(dto);
            _mapper.CalculateWinnerFor(dto);
        }

        return dto;
    }

    public async Task<int> CreateMatchAsync(CreateMatchRequest request)
    {
        _logger.LogInformation("Creating new match for game {GameId}", request.GameId);

        // Validate request
        await _validator.ValidateForCreateAsync(request);

        var match = new Match
        {
            GameId = request.GameId,
            MatchDate = request.MatchDate,
            Notes = request.Notes,
            isFinished = request.isFinished
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match created successfully with ID {MatchId}", match.Id);
        return match.Id;
    }

    public async Task UpdateMatchAsync(int id, UpdateMatchRequest request)
    {
        _logger.LogInformation("Updating match {MatchId}", id);

        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Match with ID {id} not found.");

        // Validate request
        await _validator.ValidateForUpdateAsync(match, request);

        match.GameId = request.GameId;
        match.MatchDate = request.MatchDate;
        match.Notes = request.Notes;
        match.isFinished = request.isFinished;

        _context.Matches.Update(match);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match {MatchId} updated successfully", id);
    }

    public async Task DeleteMatchAsync(int id)
    {
        _logger.LogInformation("Deleting match {MatchId}", id);

        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new NotFoundException($"Match with ID {id} not found.");

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Match {MatchId} deleted successfully", id);
    }

    /// <summary>
    /// Maps a Match entity to a MatchForMatchDto.
    /// </summary>
    private MatchForMatchDto MapToDto(Match match)
    {
        return new MatchForMatchDto
        {
            MatchId = match.Id,
            GameId = match.GameId,
            MatchDate = match.MatchDate,
            Notes = match.Notes,
            isFinished = match.isFinished,
            PlayerCount = match.PlayerCount,
            MatchDataPoints = match.MatchDataPoints?.Select(dp => new MatchDataPointForMatchDto
            {
                Id = dp.Id,
                PlayerName = dp.PlayerName,
                GamePoints = dp.GamePoints,
                PointsDescription = dp.PointsDescription,
                CreatedDate = dp.CreatedDate
            }).ToList() ?? new List<MatchDataPointForMatchDto>(),
            MatchStats = new MatchStatsDto()
        };
    }
}
