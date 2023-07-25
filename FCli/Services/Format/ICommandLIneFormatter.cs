using System.Reflection;

namespace FCli.Services.Format;

public interface ICommandLineFormatter
{
    /// <summary>
    /// Echos logo and basic helpful info about fallen-cli.
    /// </summary>
    public void EchoGreeting()
    {
        EchoLogo();
        EchoNameAndVersion();
        Console.WriteLine();
        Console.WriteLine("""
        This tool can memorize actions and execute them.
        Currently supports:
            Url
            Script
            Executable
        
        Usage:
            fcli <tool?> [params ...] [flags ...]

            fcli add C:\Awesome --exe --name awe
            fcli awe
        
        For more information about the tools use --help.
        To list all tools or commands use:
            fcli list
            fcli list --tool
        """);
    }

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
    /// Displays more helpful info then the basic greeting.
    /// </summary>
    public void EchoHelp()
    {
        EchoNameAndVersion();
        Console.WriteLine("""
            
            This tool can be used to remember apps, sites and script files, and
            call them afterwards. This should help with using command line.
            
            Syntax:
                fcli <tool/command> [flags...]
            
            Tools:
                Tools manipulate command storage and do informational work.

                add    - stores new command to storage.
                remove - deletes a known command from storage.
                list   - displays all known commands or tools.
                run    - performs a command without saving it.
            
                More detailed info about each command can be access by specifying
                special flag --help after that specific tool.
            
            Commands:
                If given argument doesn't align with a known tool or it's alias
                it is considered a command. If this command is known, then it gets
                executed in the most appropriate vay.
                All known commands can be listed using list tool.
            
            Usage:
                fcli add c:\awesome --exe -name aw
                fcli aw --options "-s 2"
                fcli remove aw
                fcli add https://google.com --name google
                fcli google
            """);
    }

    // Actual interface methods.

    /// <summary>
    /// Simply prints out the message into the console.
    /// </summary>
    /// <param name="message">String to echo out.</param>
    public void DisplayMessage(string message);

    /// <summary>
    /// Formats string as an Info message and displays it to the console.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayInfo(string callerName, string message);

    /// <summary>
    /// Displays the string in the console as a Warning.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayWarning(string callerName, string message);

    /// <summary>
    /// Errors the given message to console.
    /// </summary>
    /// <param name="callerName">Tool or command name.</param>
    /// <param name="message">String to be printed to console.</param>
    public void DisplayError(string callerName, string message);

    /// <summary>
    /// Prints out a formatted preface and then reads user's input.
    /// </summary>
    /// <param name="preface">Usually (yes/any).</param>
    /// <returns>User input.</returns>
    public string? ReadUserInput(string preface);
}