using System.Text;

using FCli.Services.Abstractions;

namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class PrettyFormatter : ICommandLineFormatter
{
    // DI.
    private readonly IResources _resources;

    public PrettyFormatter(IResources resources)
    {
        _resources = resources;
    }

    // Private data.
    private bool _progress;

    /// <summary>
    /// Loads basic info from the resources.
    /// </summary>
    public void EchoGreeting()
    {
        ((ICommandLineFormatter)this).EchoLogo();
        ((ICommandLineFormatter)this).EchoNameAndVersion();
        DisplayMessage(_resources.GetLocalizedString("Basic_Help"));
    }

    /// <summary>
    /// Loads full help page for the entire fallen-cli from the resources.
    /// </summary>
    public void EchoHelp()
    {
        ((ICommandLineFormatter)this).EchoNameAndVersion();
        DisplayMessage(_resources.GetLocalizedString("Full_Help"));
    }

    /// <summary>
    /// Writes the message in the original format.
    /// </summary>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayMessage(string? message)
        => Console.WriteLine(message);

    /// <summary>
    /// Formats Info with first line as green caller name and second as message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Info_Pretty")}: ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Formats Waring using yellow caller name and indented message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Warning_Pretty")}: ");
        Console.WriteLine(TabbedMessage(message));
        Console.ResetColor();
    }

    /// <summary>
    /// Formats Error as two lines from red caller name and indented yellow message.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string? callerName, string? message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(
            $"[{callerName}] {_resources.GetLocalizedString("FCli_Error_Pretty")}: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(TabbedMessage(message));
        Console.ResetColor();
    }

    /// <summary>
    /// Formats input as a single line with a yellow preface.
    /// </summary>
    /// <param name="preface">String that is written before input.</param>
    /// <param name="hideInput">If true hides user input.</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string? preface, bool hideInput = false)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(preface + ": ");
        Console.ResetColor();

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
    /// Draws progress graphic using arrow stile.
    /// </summary>
    /// <param name="cancellationToken">Used to stop the progress.</param>
    public Task DrawProgressAsync(CancellationToken cancellationToken) =>
        new(async () =>
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
                    // Draw the arrow.
                    for (int i = 1; i <= 10; i++)
                    {
                        Console.SetCursorPosition(Left, Top);
                        Console.Write(
                            _resources.GetLocalizedString("FCli_Progress_Pretty"));
                        Console.Write(string.Join("", Enumerable.Repeat("#", i)));
                        Console.Write(">");
                        await Task.Delay(200, cancellationToken);
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
    /// Adds tabs before each line of the string.
    /// </summary>
    /// <param name="message">String to be tabbed.</param>
    /// <returns>Tabbed message.</returns>
    private static string TabbedMessage(string? message)
    {
        var lines = message?.Split(Environment.NewLine);
        if (lines == null)
            return string.Empty;
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append('\t');
            builder.AppendLine(line);
        }
        return builder.ToString();
    }
}