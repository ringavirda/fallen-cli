// FCli namespaces.
using FCli.Exceptions;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that validates and adds new commands to storage.
/// </summary>
public class AddTool : Tool
{
    // From ToolExecutor.
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandFactory _commandFactory;
    private readonly ICommandLoader _commandLoader;

    public AddTool(
        ICommandLineFormatter formatter,
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ICommandLoader commandLoader)
        : base(formatter)
    {
        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
        _commandLoader = commandLoader;
    }

    public override string Name => "Add";
    public override string Description => """
        Add - validates a new command and adds it to the storage.
        Requires a valid path or url as an argument.
        Flags:
            --script <shell> - the path points to the script file.
            --exe            - the path points to the executable.
            --url            - the argument is a url.
            --name <value>   - explicitly specify the name for the command.
            --options <args> - options to run exe or script with.
            --help           - show description.
        Usage:
            fcli add c:/awesome.exe
            fcli add .\scripts\script --script bash --name sc
        """;
    public override List<string> Selectors => new() { "add", "a" };
    public override ToolType Type => ToolType.Add;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayInfo(Name, Description);
                return;
            }
            // Guard against empty path/url.
            if (arg == string.Empty)
            {
                _formatter.DisplayWarning(
                    Name,
                    "Add tool requires an argument - a path or url.");
                throw new ArgumentException(
                    "Add tool was called without an argument.");
            }
            // Guard against multiple type flags.
            if (flags
                .Select(f => f.Key)
                .Intersect(_toolExecutor.KnownTypeFlags)
                .Count() > 1)
            {
                _formatter.DisplayWarning(
                    Name,
                    "Add tool can accept only one of the type flags.");
                throw new FlagException(
                    "Attempted to pass multiple arguments into the add tool.");
            }
            // Forward declare future command properties.
            var name = string.Empty;
            var type = CommandType.None;
            var options = string.Empty;
            // Handle flags.
            foreach (var flag in flags)
            {
                // Set custom command name.
                if (flag.Key == "name")
                {
                    FlagHasValue(flag, Name);
                    name = flag.Value;
                }
                // Specify command line args to run command with.
                else if (flag.Key == "options")
                {
                    FlagHasValue(flag, Name);
                    options = flag.Value;
                }
                // Force set executable type.
                else if (flag.Key == "exe")
                {
                    FlagHasNoValue(flag, Name);
                    // Guard against bad path and convert to absolute.
                    arg = ValidatePath(arg, Name);
                    type = CommandType.Executable;
                }
                // Force set website type.
                else if (flag.Key == "url")
                {
                    FlagHasNoValue(flag, Name);
                    // Guard against invalid url.
                    ValidateUrl(arg, Name);
                    type = CommandType.Url;
                }
                // Force set script flavour.
                else if (flag.Key == "script")
                {
                    FlagHasValue(flag, Name);
                    // Parse actual script type.
                    try
                    {
                        type = flag.Value switch
                        {
                            "cmd" => CommandType.CMD,
                            "powershell" => CommandType.Powershell,
                            "bash" => CommandType.Bash,
                            _ => throw new ArgumentException(
                                $"Wasn't able to determine shell type on ({arg}).")
                        };
                    }
                    catch (ArgumentException)
                    {
                        _formatter.DisplayWarning(Name, """
                            Script flag must also specify type of shell.
                            Supported shells: cmd, powershell, bash.
                            """);
                        throw;
                    }
                }
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
            // Attempt set name or type if those weren't specified.
            if (name == string.Empty || type == CommandType.None)
            {
                // If arg is a hyperlink.
                if (arg.StartsWith("http://") || arg.StartsWith("https://"))
                {
                    // Guard against invalid url.
                    var uri = ValidateUrl(arg, Name);
                    // Set command name equal website name.
                    var host = uri.Host.Split('.');
                    if (name == string.Empty)
                        name = host.First() == "www" ? host[1] : host[0];
                    // Set website type.
                    if (type == CommandType.None)
                        type = CommandType.Url;
                }
                // If arg is a path.
                else if (arg.Contains('/') || arg.Contains('\\'))
                {
                    // Guard against bad path and convert to absolute.
                    arg = ValidatePath(arg, Name);
                    // Extract file's name and extension.
                    var filename = Path.GetFileName(arg).Split('.');
                    var possibleExtension = filename.Last();
                    // Set command name equal file name.
                    if (name == string.Empty)
                        name = filename[0..^1].Aggregate((s1, s2) => $"{s1}{s2}");
                    // Try parse command type from the file extension.
                    if (type == CommandType.None)
                    {
                        try
                        {
                            type = possibleExtension switch
                            {
                                "exe" => CommandType.Executable,
                                "bat" => CommandType.CMD,
                                "ps1" => CommandType.Powershell,
                                "sh" => CommandType.Bash,
                                // Throw if file type isn't recognized.
                                _ => throw new ArgumentException(
                                    $"Unknown file extension ({possibleExtension}).")
                            };
                        }
                        catch (ArgumentException)
                        {
                            _formatter.DisplayWarning(Name, """
                                Couldn't recognize the type of file.
                                Please, specify it using flags:
                                    --exe
                                    --script <shell>
                                """);
                            throw;
                        }
                    }
                }
                // Throw if wan't able to determine command name and type.
                else
                {
                    _formatter.DisplayInfo(Name, """
                        The type of file wasn't determined. FCli recognizes only 
                        file path or url. You can force execution using type 
                        flags. Consult help page for more info.
                        """);
                    throw new ArgumentException(
                        $"The type of file ({arg}) wasn't determined.");
                }
            }
            // Guard against name duplication.
            if (_toolExecutor.KnownTools.Any(tool => tool.Selectors.Contains(name))
                || _commandLoader.CommandExists(name))
            {
                _formatter.DisplayError(Name, $"""
                    Name {name} is a known command or tool.
                    Use --name flag to specify explicitly a name for the command.
                    """);
                throw new ArgumentException($"Name {name} already exists.");
            }
            // Guard against Windows shells on Linux.
            if (type == CommandType.CMD
                && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _formatter.DisplayError(Name, $"""
                    Command ({name}) is interpreted as a batch file, but Linux
                    operating system is running. CMD is only supported on Windows.
                    """);
                throw new ArgumentException(
                    $"Attempted creation of a CMD command on Linux.");
            }
            if (type == CommandType.Powershell
                && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _formatter.DisplayWarning(Name, $"""
                    Command ({name}) is interpreted as a powershell script, but
                    Linux operating system is running. PS scripts can be executed
                    on Linux, but powershell needs to be installed from the packet
                    manager.
                    """);
                _formatter.DisplayMessage(
                    "Are you sure you can run a powershell script? (yes/any):");
                var response = Console.ReadLine();
                if (response?.ToLower() != "yes")
                {
                    _formatter.DisplayMessage("Averting add process...");
                    return;
                }
                else _formatter.DisplayMessage("Continuing add process...");
            }
            // Display parsed command.
            _formatter.DisplayInfo(Name, $"""
                Command was parsed:
                name    - {name}
                type    - {type}
                path    - {arg}
                options - {options}
                """);
            _formatter.DisplayMessage("Saving to storage...");
            // Construct the command using parsed values.
            var command = _commandFactory.Construct(name, arg, type, options);
            // Save the command into storage.
            _commandLoader.SaveCommand(command);
            // Display confirmation.
            _formatter.DisplayMessage($"""
                Saved.
                To use the command try:
                    fcli {name}
                """);
        };
}
