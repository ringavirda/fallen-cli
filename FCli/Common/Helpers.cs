using System.Reflection;

namespace FCli.Common;

/// <summary>
/// Static class for small methods that just return stuff or do stuff 
/// independently. Has functions to print out diffenrent helpful information.
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Echos logo and basic info about this tool to the console.
    /// </summary>
    public static void EchoGreeting()
    {
        EchoLogo();
        EchoNameAndVersion();
        Console.WriteLine();
        Console.WriteLine("""
        This tool can memorize actions and execute them.
        Curently supprorts:
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
            fcli list --tools
        """);
    }

    /// <summary>
    /// Need to remember to update versions.
    /// </summary>
    public static void EchoNameAndVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Console.WriteLine(
            $"\t{assembly
                .GetCustomAttribute<AssemblyProductAttribute>()?.Product}: v{assembly.
                    GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version}");
    }
    
    /// <summary>
    /// Everyone needs one.
    /// </summary>
    public static void EchoLogo()
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
    /// Yeah, i've finally written one for my app.
    /// </summary>
    public static void EchoHelp()
    {
        EchoNameAndVersion();
        Console.WriteLine("""
            
            This tool can be used to remember apps, sites and script files, and
            call them afterwards. This should help with using command line.
            
            Syntax:
                fcli <tool/command> [flags...]
            
            Tools:
                Tools manipulate command storage and do informational work.

                add    - stores new command to strorage.
                remove - deletes a known command from storage.
                list   - displays all known commands or tools.
                run    - performs a command without saving it.
            
                More detailed info about each command can be access by specifying
                special flag --help after that specific tool.
            
            Commands:
                If given argument doesn't allign with a known tool or it's alias
                it is considered a command. If this command is known, then it gets
                executed in the most appropriate vay.
                All known commands can be listed using list tool.
            
            Usage:
                fcli add c:\awesome --exe -name aw
                fcli aw --options "-s 2"
                fcli remove aw
                flci add https://google.com --name google
                flci google
            """);
    }
}
