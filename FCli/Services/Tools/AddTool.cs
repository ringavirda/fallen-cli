// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that validates and adds new commands to storage.
/// </summary>
public class AddTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandLoader _commandLoader;
    private readonly ICommandFactory _commandFactory;

    public AddTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandLoader commandLoader,
        ICommandFactory commandFactory)
        : base(formatter, resources)
    {
        _commandFactory = commandFactory;
        _commandLoader = commandLoader;
        _config = config;

        Description = resources.GetLocalizedString("Add_Help");
    }

    // Private data.
    private readonly CommandAlterRequest _creationRequest = new();

    // Overrides.

    public override string Name => "Add";
    public override string Description { get; }
    public override List<string> Selectors => new() { "add", "a" };
    public override ToolType Type => ToolType.Add;

    protected override void GuardInit()
    {
        // Guard against empty path/url.
        if (Arg == string.Empty)
        {
            _formatter.DisplayError(Name,
                string.Format(
                    _resources.GetLocalizedString("FCli_ArgMissing"),
                    Name));
            throw new ArgumentException(
                "[Add] No argument was given.");
        }
        // Guard against multiple type flags.
        if (Flags.Select(f => f.Key)
            .Intersect(_config.KnownCommands.Select(c => c.Selector))
            .Count() > 1)
        {
            _formatter.DisplayError(Name,
                string.Format(
                    _resources.GetLocalizedString("FCli_MultipleTypeFlags"),
                    Name));
            throw new FlagException(
                "[Add] Attempted to pass multiple command types flags.");
        }
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Set custom command name.
        if (flag.Key == "name")
        {
            FlagHasValue(flag, Name);
            _creationRequest.Name = flag.Value;
        }
        // Specify command line args to run command with.
        else if (flag.Key == "options")
        {
            FlagHasValue(flag, Name);
            _creationRequest.Options = flag.Value;
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
                    _creationRequest.Shell = shellDescriptor.Type;
                else
                {
                    _formatter.DisplayWarning(Name,
                        string.Format(
                            _resources.GetLocalizedString("FCli_UnknownShell"),
                            string.Join(", ", 
                                _config.KnownShells.Select(sh => sh.Selector))));
                    throw new ArgumentException(
                        $"[Add] Wasn't able to determine shell type on ({Arg}).");
                }
            }
            // Guard against shell execution.
            else FlagHasNoValue(flag, Name);

            // Set command type.
            _creationRequest.Type = descriptor.Type;

            // Validate path/url.
            if (_creationRequest.Type == CommandType.Website)
                ValidateUrl(Arg, Name);
            else ValidatePath(Arg, Name);
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        // Attempt set name or type if those weren't specified.
        if (_creationRequest.Name == string.Empty
            || _creationRequest.Type == CommandType.None)
        {
            // If arg is a hyperlink.
            if (Arg.StartsWith("http://") || Arg.StartsWith("https://"))
            {
                // Guard against invalid url.
                var uri = ValidateUrl(Arg, Name);
                // Set command name equal website name.
                var host = uri.Host.Split('.');
                if (_creationRequest.Name == string.Empty)
                    _creationRequest.Name = host.First() == "www"
                        ? host[1]
                        : host[0];
                // Set website type.
                if (_creationRequest.Type == CommandType.None)
                    _creationRequest.Type = CommandType.Website;
            }
            // If arg is a path.
            else if (Arg.Contains('/') || Arg.Contains('\\'))
            {
                // Guard against bad path and convert to absolute.
                Arg = ValidatePath(Arg, Name);
                // Switch between file and directory.
                if (Directory.Exists(Arg))
                {
                    // Set directory command type.
                    if (_creationRequest.Name == string.Empty)
                        _creationRequest.Name = new DirectoryInfo(Arg).Name;
                    _creationRequest.Type = CommandType.Directory;
                }
                else
                {
                    // Extract file's name and extension.
                    var filename = Path.GetFileName(Arg).Split('.');
                    var possibleExtension = filename.Last();
                    // Set command name equal file name.
                    if (_creationRequest.Name == string.Empty)
                        _creationRequest.Name = new FileInfo(Arg).Name
                            .Split('.').First();
                    // Try parse command type from the file extension.
                    if (_creationRequest.Type == CommandType.None)
                    {
                        // If top level command.
                        var commandDesc = _config.KnownCommands
                            .FirstOrDefault(
                                desc => desc.FileExtension == possibleExtension);
                        if (commandDesc != null)
                            _creationRequest.Type = commandDesc.Type;
                        // If shell script.
                        else
                        {
                            var shellDesc = _config.KnownShells
                                .FirstOrDefault(
                                    desc => desc.FileExtension == possibleExtension);
                            if (shellDesc != null)
                            {
                                // Set script type.
                                _creationRequest.Type = CommandType.Script;
                                // Set script shell.
                                _creationRequest.Shell = shellDesc.Type;
                            }
                            // Throw if command unidentified.
                            else
                            {
                                _formatter.DisplayError(Name,
                                    _resources.GetLocalizedString("Add_FileUnrecognized"));
                                throw new ArgumentException(
                                    $"[Add] Unknown file extension ({possibleExtension}).");
                            }
                        }
                    }
                }
            }
            // Throw if wan't able to determine command name and type.
            else
            {
                _formatter.DisplayError(Name,
                    _resources.GetLocalizedString("FCli_CommandNotDetermined"));
                throw new ArgumentException(
                    $"[Add] Command wasn't determined from ({Arg}).");
            }
        }
        // Guard against name duplication.
        if (_config.KnownTools.Any(
            tool => tool.Selectors.Contains(_creationRequest.Name))
            || _commandLoader.CommandExists(_creationRequest.Name))
        {
            _formatter.DisplayError(Name,
                string.Format(
                    _resources.GetLocalizedString("FCli_NameExists"),
                    _creationRequest.Name
                ));
            throw new CommandNameException(
                $"[Add] Name {_creationRequest.Name} already exists.");
        }
        // Guard against Linux shells on windows.
        if (_creationRequest.Shell == ShellType.Bash
            && Environment.OSVersion.Platform == PlatformID.Win32NT
            && !ScriptConfirm(_creationRequest.Name, "Add_BashOnWindows"))
        {
            // Exit fcli.
            return;
        }
        if (_creationRequest.Shell == ShellType.Fish
            && Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            UnsupportedScript(
                _creationRequest.Name,
                "Fish",
                "Windows",
                "Add_FishOnWindows");
        }
        // Guard against Windows shells on Linux.
        if (_creationRequest.Shell == ShellType.Cmd
            && Environment.OSVersion.Platform == PlatformID.Unix)
        {
            UnsupportedScript(
                _creationRequest.Name,
                "Cmd",
                "Linux",
                "Add_CmdOnLinux");
        }
        if (_creationRequest.Shell == ShellType.Powershell
            && Environment.OSVersion.Platform == PlatformID.Unix
            && !ScriptConfirm(_creationRequest.Name, "Add_PowershellOnLinux"))
        {
            // Exit fcli.
            return;
        }
        // Display parsed command.
        _formatter.DisplayInfo(Name,
            string.Format(
                _resources.GetLocalizedString("Add_ParsedCommand"),
                _creationRequest.Name,
                _creationRequest.Type,
                _creationRequest.Shell,
                Arg,
                _creationRequest.Options));
        _formatter.DisplayMessage(
            _resources.GetLocalizedString("FCli_Saving"));
        // Construct the command using parsed values.
        var command = _commandFactory.Construct(_creationRequest);
        // Save the command into storage.
        _commandLoader.SaveCommand(command);
        // Display confirmation.
        _formatter.DisplayInfo(Name, string.Format(
            _resources.GetLocalizedString("FCli_CommandSaved"),
            _creationRequest.Name));
    }

    // Private methods.

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
                _resources.GetLocalizedString(resourceString),
                commandName
            ));
        throw new ArgumentException(
            $"[Add] Attempted the creation of a {scriptType} command on {osName}.");
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
                _resources.GetLocalizedString(resourceString),
                commandName
            ));
        _formatter.DisplayMessage(
            _resources.GetLocalizedString("Add_OSScript_Question"));
        return UserConfirm();
    }
}
