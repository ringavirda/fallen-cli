namespace FCli.Common.Exceptions;

public class FlagException : ArgumentException
{
    public FlagException(string message)
        : base(message)
        {}
}
