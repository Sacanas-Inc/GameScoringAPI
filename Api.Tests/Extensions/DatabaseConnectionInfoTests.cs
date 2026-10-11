using GameScoringAPI.Extensions;

namespace GameScoringAPI.Tests.Extensions;

/// <summary>
/// Unit tests for the startup database logging helpers.
/// </summary>
public class DatabaseConnectionInfoTests
{
    [Fact]
    public void FromConnectionString_NeverExposesThePassword()
    {
        // Arrange
        const string connectionString = "Host=localhost;Port=5432;Database=gamescoringdb;Username=postgres;Password=super-secret";

        // Act
        var info = DatabaseConnectionInfo.FromConnectionString("DefaultConnection", connectionString);

        // Assert
        var text = info.ToString();
        Assert.DoesNotContain("super-secret", text);
        Assert.DoesNotContain("Password", text);
    }

    [Fact]
    public void FromConnectionString_MapsNonSecretParts()
    {
        // Arrange
        const string connectionString = "Host=db.example.com;Port=5433;Database=neondb;Username=neondb_owner;Password=secret";

        // Act
        var info = DatabaseConnectionInfo.FromConnectionString("DatabaseUrl", connectionString);

        // Assert
        Assert.Equal("DatabaseUrl", info.Source);
        Assert.Equal("db.example.com", info.Host);
        Assert.Equal(5433, info.Port);
        Assert.Equal("neondb", info.Database);
        Assert.Equal("neondb_owner", info.Username);
    }

    [Fact]
    public void FromConnectionString_CarriesResolutionWarning()
    {
        // Act
        var info = DatabaseConnectionInfo.FromConnectionString(
            "DefaultConnection",
            "Host=localhost;Database=db;Username=u",
            "boom");

        // Assert
        Assert.Equal("boom", info.ResolutionWarning);
    }

    [Fact]
    public void ToString_IsSafeToLog()
    {
        // Arrange
        const string connectionString = "Host=db.example.com;Port=5433;Database=neondb;Username=owner;Password=secret";

        // Act
        var text = DatabaseConnectionInfo.FromConnectionString("DatabaseUrl", connectionString).ToString();

        // Assert
        Assert.Contains("Host=db.example.com", text);
        Assert.Contains("Database=neondb", text);
        Assert.Contains("User=owner", text);
        Assert.DoesNotContain("secret", text);
    }
}

/// <summary>
/// Unit tests for <see cref="DatabaseInitializationState"/>.
/// </summary>
public class DatabaseInitializationStateTests
{
    [Fact]
    public void NewState_IsNotCompleted()
    {
        var state = new DatabaseInitializationState();

        Assert.False(state.Completed);
        Assert.False(state.Succeeded);
        Assert.Null(state.Error);
    }

    [Fact]
    public void MarkSucceeded_SetsCompletedAndClearsError()
    {
        var state = new DatabaseInitializationState();
        state.MarkFailed("previous failure");

        state.MarkSucceeded();

        Assert.True(state.Completed);
        Assert.True(state.Succeeded);
        Assert.Null(state.Error);
    }

    [Fact]
    public void MarkFailed_RecordsError()
    {
        var state = new DatabaseInitializationState();

        state.MarkFailed("connection refused");

        Assert.True(state.Completed);
        Assert.False(state.Succeeded);
        Assert.Equal("connection refused", state.Error);
    }
}
