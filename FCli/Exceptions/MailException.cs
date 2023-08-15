namespace FCli.Exceptions;

public class MailException : IdentityException
{
    public MailException() { }
    public MailException(string? message)
        : base(message) { }
    public MailException(string? message, Exception? innerException)
        : base(message, innerException) { }
}