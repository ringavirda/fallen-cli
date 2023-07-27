namespace FCli.Exceptions;

/// <summary>
/// Abstracts all situations when something is wrong with a command name.
/// </summary>
public class CommandNameException : ArgumentException
{
    public CommandNameException()
        : base() { }
    public CommandNameException(string message)
        : base(message) { }
    public CommandNameException(string message, Exception innerException)
        : base(message, innerException) { }
}