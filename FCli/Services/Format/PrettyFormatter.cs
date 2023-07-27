﻿namespace FCli.Services.Format;

/// <summary>
/// Command line formatter that uses multiline messages and colors.
/// </summary>
public class PrettyFormatter : ICommandLineFormatter
{
    /// <summary>
    /// Writes the message in the original format.
    /// </summary>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayMessage(string? message)
        => Console.WriteLine(message
            ?? ((ICommandLineFormatter)this).StringNotLoaded());

    /// <summary>
    /// Formats Info with first line as green caller name and second as message.
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
            Console.WriteLine($"[{callerName}] Wants to inform you:");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Formats Waring using yellow caller name and indented message.
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
            Console.WriteLine($"[{callerName}] Warns you about this:");
            Console.WriteLine(message
                .Split('\n')
                .Select(s => $"\t{s}\n")
                .Aggregate((s1, s2) => s1 + s2));
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Formats Error as two lines from red caller name and indented yellow message.
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
            Console.WriteLine($"[{callerName}] An error occurred during execution:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message
                .Split('\n')
                .Select(s => $"\t{s}\n")
                .Aggregate((s1, s2) => s1 + s2));
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Formats input as a single line with a yellow preface.
    /// </summary>
    /// <param name="preface">Usually (yes/any).</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string preface)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(preface + ": ");
        Console.ResetColor();
        return Console.ReadLine();
    }
}
