namespace FCli.Exceptions;

/// <summary>
/// Represents errors concerning the mailing system.
/// </summary>
public class MailException : IdentityException
{
    public MailException() { }
    public MailException(string? message)
        : base(message) { }
    public MailException(string? message, Exception? innerException)
        : base(message, innerException) { }
}