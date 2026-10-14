using GameScoringAPI.Extensions;
using Microsoft.AspNetCore.Builder;

namespace GameScoringAPI.Tests.Extensions;

/// <summary>
/// Unit tests for the startup database configuration guard.
/// </summary>
public class DatabaseSetupTests
{
    [Fact]
    public void ConfigureDatabaseServices_WithNoConnectionConfigured_ThrowsInvalidOperationException()
    {
        // Arrange: a fresh builder with every configuration source removed, so no
        // connection string is present even when the host environment defines one
        // (for example the ConnectionStrings__DatabaseUrl env var used by CI).
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.ConfigureDatabaseServices());

        // Assert
        Assert.Contains("No Postgres connection configured", exception.Message);
    }
}
