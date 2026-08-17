using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Validators;
using GameScoringAPI.Services.Exceptions;

namespace GameScoringAPI.Tests.Services;

/// <summary>
/// Unit tests for GameService.
/// Uses InMemory DbContext for fast, isolated testing.
/// </summary>
public class GameServiceTests
{
    private GameDBContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GameDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GameDBContext(options);
    }

    private GameService CreateGameService(GameDBContext context)
    {
        var validator = new GameValidator(context);
        var logger = new Mock<ILogger<GameService>>();
        return new GameService(context, validator, logger.Object);
    }

    #region GetAllGamesAsync Tests

    [Fact]
    public async Task GetAllGamesAsync_WithNoGames_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);

        // Act
        var result = await service.GetAllGamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllGamesAsync_WithGames_ReturnsAllGames()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game1 = new Game { GameName = "Chess", GameDescription = "Classic", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        var game2 = new Game { GameName = "Checkers", GameDescription = "Board game", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 30, MatchesCount = 0 };
        
        context.Games.AddRange(game1, game2);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);

        // Act
        var result = await service.GetAllGamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, g => g.GameName == "Chess");
        Assert.Contains(result, g => g.GameName == "Checkers");
    }

    #endregion

    #region GetGameByIdAsync Tests

    [Fact]
    public async Task GetGameByIdAsync_WithExistingId_ReturnsGame()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game = new Game { GameName = "Chess", GameDescription = "Test", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);

        // Act
        var result = await service.GetGameByIdAsync(game.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
        Assert.Equal("Chess", result.GameName);
    }

    [Fact]
    public async Task GetGameByIdAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetGameByIdAsync(999));
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region CreateGameAsync Tests

    [Fact]
    public async Task CreateGameAsync_WithValidGame_InsertsAndReturnsId()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);
        var request = new CreateGameRequest 
        { 
            GameName = "Chess", 
            GameDescription = "Classic board game",
            MinPlayers = 2, 
            MaxPlayers = 2, 
            AverageDuration = 45 
        };

        // Act
        var gameId = await service.CreateGameAsync(request);

        // Assert
        Assert.True(gameId > 0);
        var createdGame = await context.Games.FindAsync(gameId);
        Assert.NotNull(createdGame);
        Assert.Equal("Chess", createdGame.GameName);
    }

    [Fact]
    public async Task CreateGameAsync_WithEmptyGameName_ThrowsValidationException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);
        var request = new CreateGameRequest 
        { 
            GameName = "", 
            GameDescription = "Test",
            MinPlayers = 2, 
            MaxPlayers = 2, 
            AverageDuration = 45 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateGameAsync(request));
        Assert.Contains("GameName", string.Join(", ", ex.Errors));
    }

    [Fact]
    public async Task CreateGameAsync_WithDuplicateName_ThrowsValidationException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var existingGame = new Game { GameName = "Chess", GameDescription = "Test", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        context.Games.Add(existingGame);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);
        var request = new CreateGameRequest 
        { 
            GameName = "Chess",  // Duplicate name
            GameDescription = "Another chess game",
            MinPlayers = 2, 
            MaxPlayers = 2, 
            AverageDuration = 60 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateGameAsync(request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateGameAsync_WithNegativeMinPlayers_ThrowsValidationException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);
        var request = new CreateGameRequest 
        { 
            GameName = "Chess", 
            GameDescription = "Test",
            MinPlayers = -1,  // Invalid
            MaxPlayers = 2, 
            AverageDuration = 45 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateGameAsync(request));
        Assert.Contains("MinPlayers", string.Join(", ", ex.Errors));
    }

    [Fact]
    public async Task CreateGameAsync_WithMinGreaterThanMax_ThrowsValidationException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);
        var request = new CreateGameRequest 
        { 
            GameName = "Chess", 
            GameDescription = "Test",
            MinPlayers = 5, 
            MaxPlayers = 2,  // Min > Max is invalid
            AverageDuration = 45 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateGameAsync(request));
        Assert.Contains("MinPlayers", string.Join(", ", ex.Errors));
    }

    #endregion

    #region UpdateGameAsync Tests

    [Fact]
    public async Task UpdateGameAsync_WithValidUpdate_UpdatesGame()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game = new Game { GameName = "Chess", GameDescription = "Old", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);
        var request = new UpdateGameRequest 
        { 
            GameName = "Chess Updated", 
            GameDescription = "New description",
            MinPlayers = 2, 
            MaxPlayers = 4, 
            AverageDuration = 60 
        };

        // Act
        await service.UpdateGameAsync(game.Id, request);

        // Assert
        var updated = await context.Games.FindAsync(game.Id);
        Assert.NotNull(updated);
        Assert.Equal("Chess Updated", updated.GameName);
        Assert.Equal("New description", updated.GameDescription);
        Assert.Equal(60, updated.AverageDuration);
    }

    [Fact]
    public async Task UpdateGameAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);
        var request = new UpdateGameRequest 
        { 
            GameName = "Chess", 
            GameDescription = "Test",
            MinPlayers = 2, 
            MaxPlayers = 2, 
            AverageDuration = 45 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateGameAsync(999, request));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task UpdateGameAsync_WithDuplicateName_ThrowsValidationException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game1 = new Game { GameName = "Chess", GameDescription = "Test1", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        var game2 = new Game { GameName = "Checkers", GameDescription = "Test2", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 30, MatchesCount = 0 };
        context.Games.AddRange(game1, game2);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);
        var request = new UpdateGameRequest 
        { 
            GameName = "Chess",  // Try to rename game2 to game1's name
            GameDescription = "Test",
            MinPlayers = 2, 
            MaxPlayers = 2, 
            AverageDuration = 30 
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateGameAsync(game2.Id, request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UpdateGameAsync_WithSameName_Succeeds()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game = new Game { GameName = "Chess", GameDescription = "Old", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);
        var request = new UpdateGameRequest 
        { 
            GameName = "Chess",  // Same name is OK
            GameDescription = "Updated",
            MinPlayers = 2, 
            MaxPlayers = 4, 
            AverageDuration = 60 
        };

        // Act - Should not throw
        await service.UpdateGameAsync(game.Id, request);

        // Assert
        var updated = await context.Games.FindAsync(game.Id);
        Assert.Equal("Updated", updated.GameDescription);
    }

    #endregion

    #region DeleteGameAsync Tests

    [Fact]
    public async Task DeleteGameAsync_WithExistingId_DeletesGame()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var game = new Game { GameName = "Chess", GameDescription = "Test", MinPlayers = 2, MaxPlayers = 2, AverageDuration = 45, MatchesCount = 0 };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var service = CreateGameService(context);

        // Act
        await service.DeleteGameAsync(game.Id);

        // Assert
        var deleted = await context.Games.FindAsync(game.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteGameAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateGameService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteGameAsync(999));
        Assert.Contains("not found", ex.Message);
    }

    #endregion
}
