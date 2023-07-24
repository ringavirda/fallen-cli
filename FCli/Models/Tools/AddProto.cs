// FCli namespaces.
using FCli.Common;
using FCli.Services;
using FCli.Services.Data;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// Prototype for the tool that adds new commands into storage.
/// </summary>
public class AddProto : Tool, IToolProto
{
    // From ToolExecutor.
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandFactory _commandFactory;
    private readonly ICommandLoader _commandLoader;

    public AddProto(
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ICommandLoader commandLoader)
    {
        Name = "Add";
        Description = """
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
        Type = ToolType.Add;
        Selectors = new() { "add", "a" };

        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
        _commandLoader = commandLoader;
    }

    /// <summary>
    /// Constructs the ADD tool from this prototype.
    /// </summary>
    /// <returns>Tool configured for add operations.</returns>
    /// <exception cref="ArgumentException">If something is wrong with the path/url.</exception>
    /// <exception cref="FlagException">When flags are invalid.</exception>
    public Tool GetTool()
    {
        // Begin ADD logic construction.
        Action = (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                Helpers.DisplayInfo(Name, Description);
                return;
            }
            // Guard against empty path/url.
            if (arg == string.Empty)
            {
                Helpers.DisplayWarning(
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
                Helpers.DisplayWarning(
                    Name,
                    "Add tool can accept only one of the type flags.");
                throw new ArgumentException(
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
                        Helpers.DisplayWarning(Name, """
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
                            Helpers.DisplayWarning(Name, """
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
                    Helpers.DisplayInfo(Name, """
                        The type of file wasn't determined. FCli recognizes only 
                        file path or url. You can force execution using type 
                        flags. Consult help page for more info.
                        """);
                    throw new ArgumentException(
                        $"The type of file ({arg}) wasn't determined.");
                }
            }
            // Guard against name duplication.
            if (_toolExecutor.ToolProtos.Select(proto => (Tool)proto)
                .Any(tool => tool.Name == name)
                || _commandLoader.CommandExists(name))
            {
                Helpers.DisplayError(Name, $"""
                    Name {name} is a known command or tool.
                    Use --name flag to specify explicitly a name for the command.
                    """);
                throw new ArgumentException($"Name {name} already exists.");
            }
            // Display parsed command.
            Helpers.DisplayInfo(Name, $"""
                Command was parsed:
                name    - {name}
                type    - {type}
                path    - {arg}
                options - {options}
                """);
            Helpers.DisplayMessage("Saving to storage...");
            // Construct the command using parsed values.
            var command = _commandFactory.Construct(name, arg, type, options);
            // Save the command into storage.
            _commandLoader.SaveCommand(command);
            // Display confirmation.
            Helpers.DisplayMessage($"""
                Saved.
                To use the command try:
                    fcli {name}
                """);
        };
        // Return constructed tool.
        return this;
    }
}
