namespace FCli.Exceptions;

/// <summary>
/// Critical exception that get's thrown if resources are missing.
/// </summary>
public class ResourceNotLoadedException : CriticalException
{
    public ResourceNotLoadedException()
        : base() { }
    public ResourceNotLoadedException(string? message)
        : base(message) { }

    public ResourceNotLoadedException(string? message, Exception? innerException)
        : base(message, innerException) { }
}