namespace FCli.Exceptions;

/// <summary>
/// Fallen-cli root exception that represents an unexpected behavior that cannot
/// be programmatically processed. 
/// </summary>
public class CriticalException : Exception
{
    public CriticalException()
        : base(){ }
    public CriticalException(string? message) 
        : base(message) { }

    public CriticalException(string? message, Exception? innerException) 
        : base(message, innerException) { }
}
