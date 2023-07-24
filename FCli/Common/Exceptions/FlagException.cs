namespace FCli.Common.Exceptions;

/// <summary>
/// This exception has semantics of an error concerning command line flags.
/// </summary>
public class FlagException : ArgumentException
{
    public FlagException() 
        : base() { }
    public FlagException(string? message)
        : base(message) { }
    public FlagException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
