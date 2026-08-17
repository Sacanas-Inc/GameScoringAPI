namespace GameScoringAPI.Services.Exceptions;

/// <summary>
/// Thrown when business or field validation fails.
/// </summary>
public class ValidationException : ServiceException
{
    public List<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public ValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}
