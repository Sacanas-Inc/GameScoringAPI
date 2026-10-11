namespace GameScoringAPI.Extensions;

/// <summary>
/// Tracks the outcome of database initialization so the health endpoint can report it
/// even when startup continues in degraded mode (migrations failed but the app is up).
/// </summary>
public sealed class DatabaseInitializationState
{
    /// <summary>True once initialization has run (successfully or not).</summary>
    public bool Completed { get; private set; }

    /// <summary>True when migrations (and triggers) were ensured successfully.</summary>
    public bool Succeeded { get; private set; }

    /// <summary>Failure detail (no secrets) when <see cref="Succeeded"/> is false.</summary>
    public string? Error { get; private set; }

    public void MarkSucceeded()
    {
        Completed = true;
        Succeeded = true;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Completed = true;
        Succeeded = false;
        Error = error;
    }
}
