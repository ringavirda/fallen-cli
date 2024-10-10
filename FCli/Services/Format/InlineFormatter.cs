using System.Text;

using FCli.Services.Abstractions;

namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class InlineFormatter(IResources strings) : ICommandLineFormatter
{
    // DI.
    private readonly IResources _resources = strings;

    // Private data.
    private bool _progress;

    /// <summary>
    /// Loads basic info from the resources.
    /// </summary>
    public void EchoGreeting()
    {
        ((ICommandLineFormatter)this).EchoLogo();
        DisplayMessage(_resources.GetLocalizedString("Basic_Help"));
    }

    /// <summary>
    /// Loads full help page for the entire fallen-cli from the resources.
    /// </summary>
    public void EchoHelp()
        => DisplayMessage(_resources.GetLocalizedString("Full_Help"));

    /// <summary>
    /// Writes the message to the console.
    /// </summary>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayMessage(string? message)
        => Console.WriteLine(message);

    /// <summary>
    /// Formats Info as a line starting with green caller name and normal 
    /// inline message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Info_Inline")}: ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Formats Waring in one line starting with yellow caller name and ending
    /// with normal inline message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Warning_Inline")}: ");
        Console.ResetColor();
        Console.WriteLine(Inline(message));
    }

    /// <summary>
    /// Formats Error as single line with red caller name and normal message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Error_Inline")}: ");
        Console.ResetColor();
        Console.WriteLine(Inline(message));
    }

    /// <summary>
    /// Formats input line as a plain single liner.
    /// </summary>
    /// <param name="preface">String that is written before input.</param>
    /// <param name="hideInput">If true hides user input.</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string? preface, bool hideInput = false)
    {
        Console.Write(preface + ": ");
        var input = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(hideInput);
            if (key.Key == ConsoleKey.Enter)
                break;
            else input.Append(key.KeyChar);
        }
        return input.ToString();
    }

    /// <summary>
    /// Draws progress as simple loading animated message.
    /// </summary>
    /// <param name="cancellationToken">Used to stop the progress.</param>
    public Task DrawProgressAsync(CancellationToken cancellationToken)
        => new(async () =>
        {
            // Store original position.
            var (Left, Top) = Console.GetCursorPosition();
            // Setup console properties.
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Cyan;
            // Set flag.
            _progress = true;
            // Do the drawing.
            while (true)
            {
                try
                {
                    // Draw the loading message.
                    for (int i = 1; i <= 3; i++)
                    {
                        Console.SetCursorPosition(Left, Top);
                        Console.Write(
                            _resources.GetLocalizedString("FCli_Progress_Inline"));
                        Console.Write(
                            string.Join("", Enumerable.Repeat('.', i)));
                        await Task.Delay(400, cancellationToken);
                    }
                    // Clean console for the new cycle.
                    Console.SetCursorPosition(Left, Top);
                    Console.Write(
                        string.Join(" ", Enumerable.Repeat("    ", 10)));
                }
                catch (TaskCanceledException)
                {
                    // Reset console properties.
                    Console.ResetColor();
                    Console.CursorVisible = true;
                    // Reset flag.
                    _progress = false;
                    // Stop execution.
                    return;
                }
            }
        }, cancellationToken);

    /// <summary>
    /// Writes message cleanly if progress is running.
    /// </summary>
    /// <param name="message">To display.</param>
    public void DisplayProgressMessage(string? message)
    {
        if (_progress)
        {
            // Clean console.
            Console.Write(
                '\r' + string.Join(" ", Enumerable.Repeat("    ", 10)));
            // Write as line.
            Console.WriteLine($"\r{message}");
        }
    }

    /// <summary>
    /// Reformats message as a single line
    /// </summary>
    /// <param name="message">To reformat.</param>
    /// <returns>Inlined message.</returns>
    private static string Inline(string? message)
    {
        if (message == null) return string.Empty;
        var builder = new StringBuilder(message);
        builder.Replace(Environment.NewLine, " ");
        return builder.ToString();
    }
}