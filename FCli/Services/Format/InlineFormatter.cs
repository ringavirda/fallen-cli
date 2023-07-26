// Vendor namespaces.
using System.Text;

namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class InlineFormatter : ICommandLineFormatter
{
    /// <summary>
    /// Writes the message to the console.
    /// </summary>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayMessage(string? message)
        => Console.WriteLine(message 
            ?? ((ICommandLineFormatter)this).StringNotLoaded());

    /// <summary>
    /// Formats Info as a line starting with green caller name and normal 
    /// inline message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string callerName, string? message)
    {
        if (message == null)
            ((ICommandLineFormatter)this).StringNotLoaded();
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{callerName}] Info: ");
            Console.ResetColor();
            Console.WriteLine(Inline(message));
        }
    }

    /// <summary>
    /// Formats Waring in one line starting with yellow caller name and ending
    /// with normal inline message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string callerName, string? message)
    {
        if (message == null)
            ((ICommandLineFormatter)this).StringNotLoaded();
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{callerName}] Warn: ");
            Console.ResetColor();
            Console.WriteLine(Inline(message));
        }
    }

    /// <summary>
    /// Formats Error as single line with red caller name and normal message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string callerName, string? message)
    {
        if (message == null)
            ((ICommandLineFormatter)this).StringNotLoaded();
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"[{callerName}] Err: ");
            Console.ResetColor();
            Console.WriteLine(Inline(message));
        }
    }

    /// <summary>
    /// Formats input line as a plain single liner.
    /// </summary>
    /// <param name="preface">Usually (yes/any).</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string preface)
    {
        Console.Write(preface + ": ");
        return Console.ReadLine();
    }

    /// <summary>
    /// Reformats message as a single line
    /// </summary>
    /// <param name="message">To reformat.</param>
    /// <returns>Inlined message.</returns>
    private static string Inline(string message)
    {
        var builder = new StringBuilder(message);
        builder.Replace(Environment.NewLine, " ");
        return builder.ToString();
    }
}
