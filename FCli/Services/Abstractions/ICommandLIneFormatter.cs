// Vendor namespaces.
using System.Reflection;

namespace FCli.Services.Abstractions;

/// <summary>
/// Describes a service that writes formatted text into the console.
/// </summary>
/// <remarks>
/// Has some default implementations.
/// </remarks>
public interface ICommandLineFormatter
{
    /// <summary>
    /// Writes to the console assembly name and version.
    /// </summary>
    public void EchoNameAndVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Console.WriteLine(
            $"\t{assembly.GetCustomAttribute<AssemblyProductAttribute>()
            ?.Product}: v{assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()
            ?.Version}");
    }

    /// <summary>
    /// Everyone needs one.
    /// </summary>
    public void EchoLogo()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("""
            ___________      .__  .__                 
            \_   _____/____  |  | |  |   ____   ____  
            |    __) \__  \ |  | |  | _/ __ \ /    \ 
            |     \   / __ \|  |_|  |_\  ___/|   |  \
            \___  /  (____  /____/____/\___  >___|  /
                \/        \/               \/     \/ 
            """);
        Console.ResetColor();
    }

    /// <summary>
    /// Should echo logo and basic helpful info about fallen-cli.
    /// </summary>
    public void EchoGreeting();

    /// <summary>
    /// Should display even more helpful info then the basic greeting.
    /// </summary>
    public void EchoHelp();

    // Actual interface methods.

    /// <summary>
    /// Simply prints out the message into the console.
    /// </summary>
    /// <param name="message">String to echo out.</param>
    public void DisplayMessage(string? message);

    /// <summary>
    /// Formats string as an Info message and displays it to the console.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string? callerName, string? message);

    /// <summary>
    /// Displays the string in the console as a Warning.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string? callerName, string? message);

    /// <summary>
    /// Errors the given message to console.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string? callerName, string? message);

    /// <summary>
    /// Prints out a formatted preface and then reads user's input.
    /// </summary>
    /// <param name="preface">The string that is put before user input.</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string? preface);
}