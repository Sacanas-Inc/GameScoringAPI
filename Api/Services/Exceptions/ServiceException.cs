namespace GameScoringAPI.Services.Exceptions;

/// <summary>
/// Base exception for service layer errors.
/// </summary>
public class ServiceException : Exception
{
    public ServiceException(string message) : base(message) { }
    public ServiceException(string message, Exception innerException) : base(message, innerException) { }
}
