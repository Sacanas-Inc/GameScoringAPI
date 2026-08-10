namespace GameScoringAPI.Services.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found in the database.
/// </summary>
public class NotFoundException : ServiceException
{
    public NotFoundException(string message) : base(message) { }
}
