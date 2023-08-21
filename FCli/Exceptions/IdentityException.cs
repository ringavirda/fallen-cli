namespace FCli.Exceptions;

/// <summary>
/// This exception is raised when something goes bad with authentication or identity.
/// </summary>
public class IdentityException : Exception
{
    public IdentityException() { }
    public IdentityException(string? message)
        : base(message) { }
    public IdentityException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
