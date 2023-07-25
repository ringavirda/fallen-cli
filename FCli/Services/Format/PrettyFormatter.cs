namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class PrettyFormatter : ICommandLineFormatter
{
    /// <summary>
    /// Writes the message in the original format.
    /// </summary>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayMessage(string message)
        => Console.WriteLine(message);

    /// <summary>
    /// Formats Info with first line as green caller name and second as message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string callerName, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{callerName}] Informs:");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Formats Waring using yellow caller name and indented message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string callerName, string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{callerName}] Warns that something is wrong:");
        Console.WriteLine(message
            .Split('\n')
            .Select(s => $"\t{s}\n")
            .Aggregate((s1, s2) => s1 + s2));
        Console.ResetColor();
    }

    /// <summary>
    /// Formats Error as two lines from red caller name and indented yellow message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string callerName, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{callerName}] An error occurred during execution:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message
            .Split('\n')
            .Select(s => $"\t{s}\n")
            .Aggregate((s1, s2) => s1 + s2));
        Console.ResetColor();
    }
}
