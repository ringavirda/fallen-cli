using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that validates and adds new commands to storage.
/// </summary>
public class AddTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandLoader _loader;
    private readonly ICommandFactory _factory;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public AddTool() : base()
    {
        _config = null!;
        _loader = null!;
        _factory = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public AddTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandLoader commandLoader,
        ICommandFactory commandFactory)
        : base(formatter, resources)
    {
        _factory = commandFactory;
        _loader = commandLoader;
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
            Formatter.DisplayError(Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_ArgMissing"),
                    Name));
            throw new ArgumentException(
                "[Add] No argument was given.");
        }
        // Guard against multiple type flags.
        if (Flags.Select(f => f.Key)
            .Intersect(_config.KnownCommands.Select(c => c.Selector))
            .Count() > 1)
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_MultipleTypeFlags"),
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
                    Formatter.DisplayWarning(
                        Name,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.GetLocalizedString("FCli_UnknownShell"),
                            string.Join(
                                ", ",
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

    protected override Task ActionAsync()
    {
        // Attempt set name or type if those weren't specified.
        if (string.IsNullOrEmpty(_creationRequest.Name)
            || _creationRequest.Type == CommandType.None)
        {
            // If arg is a hyperlink.
            if (Arg.StartsWith("http://", StringComparison.CurrentCulture)
                || Arg.StartsWith("https://", StringComparison.CurrentCulture))
            {
                // Guard against invalid url.
                var uri = ValidateUrl(Arg, Name);
                // Set command name equal website name.
                var host = uri.Host.Split('.');
                if (string.IsNullOrEmpty(_creationRequest.Name))
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
                    if (string.IsNullOrEmpty(_creationRequest.Name))
                        _creationRequest.Name = new DirectoryInfo(Arg).Name;
                    _creationRequest.Type = CommandType.Directory;
                }
                else
                {
                    // Extract file's name and extension.
                    var fileInfo = new FileInfo(Arg);
                    var possibleExtension = fileInfo.Extension;
                    // Set command name equal file name.
                    if (string.IsNullOrEmpty(_creationRequest.Name))
                        _creationRequest.Name =
                            fileInfo.Name.Split('.').First();
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
                            Console.WriteLine(fileInfo.Extension);
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
                                Formatter.DisplayError(
                                    Name,
                                    Resources.GetLocalizedString("Add_FileUnrecognized"));
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
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("FCli_CommandNotDetermined"));
                throw new ArgumentException(
                    $"[Add] Command wasn't determined from ({Arg}).");
            }
        }
        // Guard against name duplication.
        if (_config.KnownTools.Any(
            tool => tool.Selectors.Contains(_creationRequest.Name))
            || _loader.CommandExists(_creationRequest.Name))
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_NameExists"),
                    _creationRequest.Name));
            throw new CommandNameException(
                $"[Add] Name {_creationRequest.Name} already exists.");
        }
        // Guard against Linux shells on windows.
        if (_creationRequest.Shell == ShellType.Bash
            && Environment.OSVersion.Platform == PlatformID.Win32NT
            && !ScriptConfirm(_creationRequest.Name, "Add_BashOnWindows"))
        {
            // Exit fcli.
            return Task.CompletedTask;
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
            return Task.CompletedTask;
        }
        // Display parsed command.
        _creationRequest.Path = Arg;
        Formatter.DisplayInfo(
            Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString("Add_ParsedCommand"),
                _creationRequest.Name,
                _creationRequest.Type,
                _creationRequest.Shell,
                _creationRequest.Path,
                _creationRequest.Options));
        Formatter.DisplayMessage(
            Resources.GetLocalizedString("FCli_Saving"));
        // Construct the command using parsed values.
        var command = _factory.Construct(_creationRequest);
        // Save the command into storage.
        _loader.SaveCommand(command);
        // Display confirmation.
        Formatter.DisplayInfo(
            Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString("FCli_CommandSaved"),
                _creationRequest.Name));
        // Final.
        return Task.CompletedTask;
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
        Formatter.DisplayError(Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString(resourceString),
                commandName));
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
        Formatter.DisplayWarning(Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString(resourceString),
                commandName));
        Formatter.DisplayMessage(
            Resources.GetLocalizedString("Add_OSScript_Question"));
        return UserConfirm();
    }
}