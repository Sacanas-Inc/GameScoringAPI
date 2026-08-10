using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Services.Validators;
using GameScoringAPI.Mapper;
using GameScoringAPI.Endpoints.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameScoringAPI.Tests.Services;

public class MatchServiceTests
{
    private DbContextOptions<GameDBContext> GetInMemoryOptions()
    {
        return new DbContextOptionsBuilder<GameDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private GameDBContext CreateContext(DbContextOptions<GameDBContext> options)
    {
        var context = new GameDBContext(options);
        // Seed with a test game
        var testGame = new Game
        {
            Id = 1,
            GameName = "Test Game",
            GameDescription = "Test Description",
            MinPlayers = 2,
            MaxPlayers = 4,
            AverageDuration = 30
        };
        context.Games.Add(testGame);
        context.SaveChanges();
        return context;
    }

    private IMatchService CreateService(GameDBContext context, IGameService gameService)
    {
        var loggerMock = new Mock<ILogger<MatchService>>();
        var validatorLoggerMock = new Mock<ILogger<MatchValidator>>();
        var mapper = new MatchMapper();
        var validator = new MatchValidator(context, gameService, validatorLoggerMock.Object);
        return new MatchService(context, validator, mapper, loggerMock.Object);
    }

    [Fact]
    public async Task GetAllMatchesAsync_WithMatches_ReturnsAllMatches()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        gameServiceMock.Setup(g => g.GetGameByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new GameDto { Id = 1, GameName = "Test Game" });

        var service = CreateService(context, gameServiceMock.Object);

        var match1 = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 0 };
        var match2 = new Match { GameId = 1, MatchDate = DateTime.Now.AddDays(1), isFinished = false, PlayerCount = 0 };
        context.Matches.AddRange(match1, match2);
        context.SaveChanges();

        // Act
        var result = await service.GetAllMatchesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetMatchByIdAsync_WithExistingId_ReturnsMatch()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        var match = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 0 };
        context.Matches.Add(match);
        context.SaveChanges();

        // Act
        var result = await service.GetMatchByIdAsync(match.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(match.Id, result.MatchId);
        Assert.Equal(1, result.GameId);
    }

    [Fact]
    public async Task GetMatchByIdAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMatchByIdAsync(999));
    }

    [Fact]
    public async Task GetMatchWithWinnerAsync_WithDataPoints_CalculatesWinner()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        var match = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 2 };
        context.Matches.Add(match);
        context.SaveChanges();

        var dp1 = new MatchDataPoint { MatchId = match.Id, PlayerName = "Alice", GamePoints = 100, PointsDescription = "Alice points" };
        var dp2 = new MatchDataPoint { MatchId = match.Id, PlayerName = "Bob", GamePoints = 50, PointsDescription = "Bob points" };
        context.MatchDataPoints.AddRange(dp1, dp2);
        context.SaveChanges();

        // Act
        var result = await service.GetMatchWithWinnerAsync(match.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.MatchStats);
        Assert.Equal("Alice", result.MatchStats.WinningPlayer);
    }

    [Fact]
    public async Task CreateMatchAsync_WithValidRequest_CreatesMatch()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        gameServiceMock.Setup(g => g.GetGameByIdAsync(1))
            .ReturnsAsync(new GameDto { Id = 1, GameName = "Test Game" });

        var service = CreateService(context, gameServiceMock.Object);

        var request = new CreateMatchRequest
        {
            GameId = 1,
            MatchDate = DateTime.Now,
            Notes = "Test match",
            isFinished = false
        };

        // Act
        var result = await service.CreateMatchAsync(request);

        // Assert
        Assert.True(result > 0);
        var match = await context.Matches.FindAsync(result);
        Assert.NotNull(match);
        Assert.Equal(1, match.GameId);
    }

    [Fact]
    public async Task CreateMatchAsync_WithInvalidGameId_ThrowsValidationException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        gameServiceMock.Setup(g => g.GetGameByIdAsync(999))
            .ThrowsAsync(new NotFoundException("Game not found"));

        var service = CreateService(context, gameServiceMock.Object);

        var request = new CreateMatchRequest
        {
            GameId = 999,
            MatchDate = DateTime.Now,
            Notes = "Test match",
            isFinished = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateMatchAsync(request));
    }

    [Fact]
    public async Task UpdateMatchAsync_WithValidData_UpdatesMatch()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        gameServiceMock.Setup(g => g.GetGameByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new GameDto { Id = 1, GameName = "Test Game" });

        var service = CreateService(context, gameServiceMock.Object);

        var match = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 0 };
        context.Matches.Add(match);
        context.SaveChanges();

        var request = new UpdateMatchRequest
        {
            GameId = 1,
            MatchDate = DateTime.Now.AddDays(1),
            Notes = "Updated notes",
            isFinished = true
        };

        // Act
        await service.UpdateMatchAsync(match.Id, request);

        // Assert
        var updated = await context.Matches.FindAsync(match.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.True(updated.isFinished);
    }

    [Fact]
    public async Task UpdateMatchAsync_WithGameIdChangeAndDataPoints_ThrowsValidationException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        // Seed another game
        var game2 = new Game
        {
            Id = 2,
            GameName = "Another Game",
            GameDescription = "Another Description",
            MinPlayers = 2,
            MaxPlayers = 4,
            AverageDuration = 30
        };
        context.Games.Add(game2);
        context.SaveChanges();

        var gameServiceMock = new Mock<IGameService>();
        gameServiceMock.Setup(g => g.GetGameByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new GameDto { Id = 1, GameName = "Test Game" });

        var service = CreateService(context, gameServiceMock.Object);

        var match = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 1 };
        context.Matches.Add(match);
        context.SaveChanges();

        var dataPoint = new MatchDataPoint { MatchId = match.Id, PlayerName = "Alice", GamePoints = 10, PointsDescription = "Test points" };
        context.MatchDataPoints.Add(dataPoint);
        context.SaveChanges();

        var request = new UpdateMatchRequest
        {
            GameId = 2,
            MatchDate = DateTime.Now,
            Notes = "Test",
            isFinished = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateMatchAsync(match.Id, request));
    }

    [Fact]
    public async Task UpdateMatchAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        var request = new UpdateMatchRequest
        {
            GameId = 1,
            MatchDate = DateTime.Now,
            Notes = "Test",
            isFinished = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateMatchAsync(999, request));
    }

    [Fact]
    public async Task DeleteMatchAsync_WithExistingId_DeletesMatch()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        var match = new Match { GameId = 1, MatchDate = DateTime.Now, isFinished = false, PlayerCount = 0 };
        context.Matches.Add(match);
        context.SaveChanges();

        // Act
        await service.DeleteMatchAsync(match.Id);

        // Assert
        var deleted = await context.Matches.FindAsync(match.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteMatchAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var gameServiceMock = new Mock<IGameService>();
        var service = CreateService(context, gameServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteMatchAsync(999));
    }
}
