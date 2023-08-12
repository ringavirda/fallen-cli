﻿// Vendor namespaces.
using System.Text;
// FCli namespaces.
using FCli.Services.Abstractions;

namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class InlineFormatter : ICommandLineFormatter
{
    // DI.
    private readonly IResources _resources;

    public InlineFormatter(IResources strings)
    {
        _resources = strings;
    }

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
    /// <param name="preface">Usually (yes/any).</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string? preface)
    {
        Console.Write(preface + ": ");
        return Console.ReadLine();
    }

    public Task DrawProgressAsync(CancellationToken cancellationToken)
        => new Task(async () =>
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Cyan;
            while (true)
            {
                try
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        Console.Write("\r"
                            + _resources.GetLocalizedString("FCli_Progress_Inline"));
                        Console.Write(string.Join("", Enumerable.Repeat('.', i)));
                        await Task.Delay(400, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine(
                            "\r" + string.Join(" ", Enumerable.Repeat("    ", 10)));
                    Console.ResetColor();
                    Console.CursorVisible = true;
                    return;
                }
            }
        }, cancellationToken);
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
