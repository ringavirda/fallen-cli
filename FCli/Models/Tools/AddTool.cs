// Vendor namespaces.
using System.Resources;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services;
using FCli.Services.Config;
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
    private readonly IConfig _config;

    public AddTool(
        ICommandLineFormatter formatter,
        ResourceManager manager,
        IToolExecutor toolExecutor,
        ICommandFactory commandFactory,
        ICommandLoader commandLoader,
        IConfig config)
        : base(formatter, manager)
    {
        _toolExecutor = toolExecutor;
        _commandFactory = commandFactory;
        _commandLoader = commandLoader;
        _config = config;

        Description = _resources.GetString("Add_Help")
            ?? formatter.StringNotLoaded();
    }

    public override string Name => "Add";
    public override string Description { get; }
    public override List<string> Selectors => new() { "add", "a" };
    public override ToolType Type => ToolType.Add;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against empty path/url.
            if (arg == string.Empty)
            {
                _formatter.DisplayError(
                    Name,
                    _resources.GetString("Add_NoArg"));
                throw new ArgumentException(
                    "Add tool was called without an argument.");
            }
            // Guard against multiple type flags.
            if (flags.Select(f => f.Key)
                .Intersect(_config.KnownCommands.Select(c => c.Selector))
                .Count() > 1)
            {
                _formatter.DisplayError(
                    Name,
                    _resources.GetString("Add_MultipleTypeFlags"));
                throw new FlagException(
                    "Attempted to pass multiple command types flags into the Add tool.");
            }
            // Forward declare future command properties.
            var name = string.Empty;
            var type = CommandType.None;
            var shell = ShellType.None;
            var options = string.Empty;
            // Parse flags.
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
                else if (_config.KnownCommands.Any(c => c.Selector == flag.Key))
                {
                    var descriptor = _config.KnownCommands
                        .First(c => c.Selector == flag.Key);
                    // Check if this command a shell one,
                    if (descriptor.IsShell)
                    {
                        // Guard against no shell specified.
                        FlagHasValue(flag, Name);
                        var shellDescriptor = _config.KnownShells
                            .FirstOrDefault(sh => sh.Selector == flag.Value);
                        // Guard against unknown shell.
                        if (shellDescriptor != null)
                            shell = shellDescriptor.Type;
                        else
                        {
                            _formatter.DisplayWarning(Name,
                                string.Format(
                                    _resources.GetString("FCli_UnknownShell")
                                    ?? _formatter.StringNotLoaded(),
                                    string.Join(
                                        ", ", 
                                        _config.KnownShells.Select(sh => sh.Selector)))
                                );
                            throw new ArgumentException(
                                $"Wasn't able to determine shell type on ({arg}).");
                        }
                    }
                    // Guard against shell execution.
                    else FlagHasNoValue(flag, Name);

                    // Set command type.
                    type = descriptor.Type;

                    // Validate path/url.
                    if (type == CommandType.Website)
                        ValidateUrl(arg, Name);
                    else ValidatePath(arg, Name);
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
                        type = CommandType.Website;
                }
                // If arg is a path.
                else if (arg.Contains('/') || arg.Contains('\\'))
                {
                    // Guard against bad path and convert to absolute.
                    arg = ValidatePath(arg, Name);
                    // Switch between file and directory.
                    if (Directory.Exists(arg))
                    {
                        // Set directory command type.
                        type = CommandType.Directory;
                    }
                    else
                    {
                        // Extract file's name and extension.
                        var filename = Path.GetFileName(arg).Split('.');
                        var possibleExtension = filename.Last();
                        // Set command name equal file name.
                        if (name == string.Empty)
                            name = filename[0..^1].Aggregate((s1, s2) => $"{s1}{s2}");
                        // Try parse command type from the file extension.
                        if (type == CommandType.None)
                        {
                            // If top level command.
                            var commandDesc = _config.KnownCommands
                                .Where(desc => desc.FileExtension == possibleExtension)
                                .FirstOrDefault();
                            if (commandDesc != null)
                                type = commandDesc.Type;
                            // If shell script.
                            else
                            {
                                var shellDesc = _config.KnownShells
                                    .Where(desc => desc.FileExtension == possibleExtension)
                                    .FirstOrDefault();
                                if (shellDesc != null)
                                {
                                    // Set script type.
                                    type = CommandType.Script;
                                    // Set script shell.
                                    shell = shellDesc.Type;
                                }
                                // Throw if command unidentified.
                                else
                                {
                                    _formatter.DisplayError(Name, 
                                        _resources.GetString("Add_FileUnrecognized"));
                                    throw new ArgumentException(
                                        $"Unknown file extension ({possibleExtension}).");
                                }
                            }
                        }
                    }
                }
                // Throw if wan't able to determine command name and type.
                else
                {
                    _formatter.DisplayError(Name,
                        _resources.GetString("Add_CommandNotDetermined"));
                    throw new ArgumentException(
                        $"Command wasn't determined from ({arg}).");
                }
            }
            // Guard against name duplication.
            if (_toolExecutor.Tools.Any(tool => tool.Selectors.Contains(name))
                || _commandLoader.CommandExists(name))
            {
                _formatter.DisplayError(Name, 
                    string.Format(
                        _resources.GetString("Add_NameAlreadyExists")
                        ?? _formatter.StringNotLoaded(),
                        name
                    ));
                throw new ArgumentException($"Name {name} already exists.");
            }
            // Guard against Linux shells on windows.
            if (shell == ShellType.Bash
                && Environment.OSVersion.Platform == PlatformID.Win32NT
                && ScriptConfirm(name, "Add_BashOnWindows"))
            {
                // Exit fcli.
                return;
            }
            if (shell == ShellType.Fish
                && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                UnsupportedScript(name, "Fish", "Windows", "Add_FishOnWindows");
            }
            // Guard against Windows shells on Linux.
            if (shell == ShellType.Cmd
                && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                UnsupportedScript(name, "Cmd", "Linux", "Add_CmdOnLinux");
            }
            if (shell == ShellType.Powershell
                && Environment.OSVersion.Platform == PlatformID.Unix
                && ScriptConfirm(name, "Add_PowershellOnLinux"))
            {
                // Exit fcli.
                return;
            }
            // Display parsed command.
            _formatter.DisplayInfo(Name, 
                string.Format(
                    _resources.GetString("Add_ParsedCommand")
                    ?? _formatter.StringNotLoaded(),
                    name, type, shell, arg, options
                ));
            _formatter.DisplayMessage(
                _resources.GetString("Add_Saving"));
            // Construct the command using parsed values.
            var command = _commandFactory.Construct(
                name,
                arg,
                type,
                shell,
                options);
            // Save the command into storage.
            _commandLoader.SaveCommand(command);
            // Display confirmation.
            _formatter.DisplayMessage(string.Format(
                _resources.GetString("Add_Saved")
                ?? _formatter.StringNotLoaded(),
                name
            ));
        };
    
    /// <summary>
    /// Prevents creation of commands unsupported by operating system.
    /// </summary>
    private void UnsupportedScript(
        string commandName,
        string scriptType,
        string osName,
        string resourceString)
    {
        _formatter.DisplayError(Name, 
            string.Format(
                _resources.GetString(resourceString)
                ?? _formatter.StringNotLoaded(),
                commandName
            ));
        throw new ArgumentException(
            $"Attempted creation of a {scriptType} command on {osName}.");
    }

    /// <summary>
    /// Confirm user's intention of creating maybe unsupported script command.
    /// </summary>
    /// <returns>True if confirmed.</returns>
    private bool ScriptConfirm(
        string commandName,
        string resourceString)
    {
        _formatter.DisplayWarning(Name, 
            string.Format(
                _resources.GetString(resourceString)
                ?? _formatter.StringNotLoaded(),
                commandName
            ));
        _formatter.DisplayMessage(
            _resources.GetString("Add_OSScript_Question"));
        var response = _formatter.ReadUserInput("(yes/any)");
        if (response?.ToLower() != "yes")
        {
            _formatter.DisplayMessage(
                _resources.GetString("Add_OSScript_Avert"));
            return false;
        }
        else 
        {
            _formatter.DisplayMessage(
                _resources.GetString("Add_OSScript_Continue"));
            return true;
        }
    }
}
