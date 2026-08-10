using GameScoringAPI.Services;
using GameScoringAPI.Services.Exceptions;
using GameScoringAPI.Services.Validators;
using GameScoringAPI.Endpoints.MatchDataPoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameScoringAPI.Tests.Services;

public class MatchDataPointServiceTests
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
        // Seed with test data
        var testGame = new Game
        {
            Id = 1,
            GameName = "Test Game",
            GameDescription = "Test Description",
            MaxPlayers = 4,
            MinPlayers = 2,
            MatchesCount = 0
        };

        var testMatch = new Match
        {
            Id = 1,
            GameId = 1,
            MatchDate = DateTime.UtcNow,
            Notes = "Test match",
            PlayerCount = 0,
            isFinished = false
        };

        context.Games.Add(testGame);
        context.Matches.Add(testMatch);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task GetAllMatchDataPointsAsync_WithNoDataPoints_ReturnsEmptyList()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);
        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var result = await service.GetAllMatchDataPointsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMatchDataPointsAsync_WithDataPoints_ReturnsAll()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var result = await service.GetAllMatchDataPointsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Player 1", result.First().PlayerName);
    }

    [Fact]
    public async Task GetAllMatchDataPointsDetailedAsync_IncludesGameAndMatchInfo()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var result = await service.GetAllMatchDataPointsDetailedAsync();

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Equal("Test Game", dto.GameName);
        Assert.Equal(1, dto.GameId);
        Assert.False(dto.isMatchFinished);
    }

    [Fact]
    public async Task GetMatchDataPointByIdAsync_WithExistingId_ReturnsDataPoint()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var result = await service.GetMatchDataPointByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Player 1", result.PlayerName);
        Assert.Equal(10, result.GamePoints);
    }

    [Fact]
    public async Task GetMatchDataPointByIdAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMatchDataPointByIdAsync(999));
    }

    [Fact]
    public async Task GetDataPointsByMatchIdAsync_WithExistingMatch_ReturnsAllDataPoints()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint1 = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };

        var dataPoint2 = new MatchDataPoint
        {
            Id = 2,
            MatchId = 1,
            PlayerName = "Player 2",
            GamePoints = 5,
            PointsDescription = "Second Place",
            CreatedDate = DateTime.UtcNow
        };

        context.MatchDataPoints.Add(dataPoint1);
        context.MatchDataPoints.Add(dataPoint2);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var result = await service.GetDataPointsByMatchIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateMatchDataPointAsync_WithValidData_InsertsAndReturnsId()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var request = new CreateMatchDataPointRequest
        {
            MatchId = 1,
            PlayerName = "New Player",
            GamePoints = 15,
            PointsDescription = "Excellent"
        };

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        var id = await service.CreateMatchDataPointAsync(request);

        // Assert
        Assert.True(id > 0);
        var created = await context.MatchDataPoints.FindAsync(id);
        Assert.NotNull(created);
        Assert.Equal("New Player", created.PlayerName);
        Assert.Equal(15, created.GamePoints);
    }

    [Fact]
    public async Task CreateMatchDataPointAsync_WithValidationError_ThrowsValidationException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var request = new CreateMatchDataPointRequest
        {
            MatchId = 1,
            PlayerName = "New Player",
            GamePoints = 15,
            PointsDescription = "Excellent"
        };

        var validator = new Mock<IMatchDataPointValidator>();
        validator.Setup(v => v.ValidateForCreateAsync(It.IsAny<CreateMatchDataPointRequest>()))
            .ThrowsAsync(new ValidationException(new List<string> { "Invalid data" }));

        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateMatchDataPointAsync(request));
    }

    [Fact]
    public async Task UpdateMatchDataPointAsync_WithExistingId_UpdatesSuccessfully()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateMatchDataPointRequest
        {
            PlayerName = "Updated Player",
            GamePoints = 20
        };

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        await service.UpdateMatchDataPointAsync(1, updateRequest);

        // Assert
        var updated = await context.MatchDataPoints.FindAsync(1);
        Assert.Equal("Updated Player", updated.PlayerName);
        Assert.Equal(20, updated.GamePoints);
    }

    [Fact]
    public async Task UpdateMatchDataPointAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var updateRequest = new UpdateMatchDataPointRequest
        {
            PlayerName = "Updated Player"
        };

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateMatchDataPointAsync(999, updateRequest));
    }

    [Fact]
    public async Task UpdateMatchDataPointAsync_WithValidationError_ThrowsValidationException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateMatchDataPointRequest
        {
            PlayerName = "Updated Player"
        };

        var validator = new Mock<IMatchDataPointValidator>();
        validator.Setup(v => v.ValidateForUpdateAsync(It.IsAny<MatchDataPoint>(), It.IsAny<UpdateMatchDataPointRequest>()))
            .ThrowsAsync(new ValidationException(new List<string> { "Invalid update" }));

        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateMatchDataPointAsync(1, updateRequest));
    }

    [Fact]
    public async Task DeleteMatchDataPointAsync_WithExistingId_DeletesSuccessfully()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };
        context.MatchDataPoints.Add(dataPoint);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        await service.DeleteMatchDataPointAsync(1);

        // Assert
        var deleted = await context.MatchDataPoints.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteMatchDataPointAsync_WithMissingId_ThrowsNotFoundException()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteMatchDataPointAsync(999));
    }

    [Fact]
    public async Task DeleteAllDataPointsForMatchAsync_WithExistingMatch_DeletesAll()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var dataPoint1 = new MatchDataPoint
        {
            Id = 1,
            MatchId = 1,
            PlayerName = "Player 1",
            GamePoints = 10,
            PointsDescription = "Victory",
            CreatedDate = DateTime.UtcNow
        };

        var dataPoint2 = new MatchDataPoint
        {
            Id = 2,
            MatchId = 1,
            PlayerName = "Player 2",
            GamePoints = 5,
            PointsDescription = "Second Place",
            CreatedDate = DateTime.UtcNow
        };

        context.MatchDataPoints.Add(dataPoint1);
        context.MatchDataPoints.Add(dataPoint2);
        await context.SaveChangesAsync();

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act
        await service.DeleteAllDataPointsForMatchAsync(1);

        // Assert
        var remaining = await context.MatchDataPoints.Where(dp => dp.MatchId == 1).CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task DeleteAllDataPointsForMatchAsync_WithNoDataPoints_DoesNotThrow()
    {
        // Arrange
        var options = GetInMemoryOptions();
        var context = CreateContext(options);

        var validator = new Mock<IMatchDataPointValidator>();
        var logger = new Mock<ILogger<MatchDataPointService>>();
        var service = new MatchDataPointService(context, validator.Object, logger.Object);

        // Act & Assert - should not throw
        await service.DeleteAllDataPointsForMatchAsync(1);
    }
}
