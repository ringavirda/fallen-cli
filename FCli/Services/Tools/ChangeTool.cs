using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

public class ChangeTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandFactory _factory;
    private readonly ICommandLoader _loader;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public ChangeTool() : base()
    {
        _config = null!;
        _factory = null!;
        _loader = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public ChangeTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandLoader loader,
        ICommandFactory factory)
        : base(formatter, resources)
    {
        _loader = loader;
        _factory = factory;
        _config = config;

        Description = resources.GetLocalizedString("Change_Help");
    }

    // Private data.
    private Command _command = null!;
    private CommandAlterRequest _changeRequest = new();

    //Overrides.

    public override string Name => "Change";
    public override string Description { get; }
    public override List<string> Selectors => new() { "change", "ch" };
    public override ToolType Type => ToolType.Change;

    protected override void GuardInit()
    {
        // Guard against no arg.
        if (Arg == string.Empty)
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_ArgMissing"),
                    Arg));
            throw new ArgumentException("[Change] Tried to change nothing.");
        }
        // Guard against invalid command name.
        var command = _loader.LoadCommand(Arg);
        if (command == null)
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_UnknownName"),
                    Name));
            throw new CommandNameException(
                "[Change] Name was invalid.");
        }
        // Init command and request if valid.
        _command = command;
        _changeRequest = _command.ToAlterRequest();
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // All change flags have values.
        FlagHasValue(flag, Name);

        // Change command name.
        if (flag.Key == "name")
        {
            // Guard against known name.
            if (_loader.CommandExists(flag.Value)
                || _config.KnownTools.Any(
                    tool => tool.Selectors.Contains(flag.Value)))
            {
                Formatter.DisplayError(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("FCli_NameExists"),
                        flag.Value));
                throw new CommandNameException(
                    "[Change] Tried to create a command with existing name.");
            }
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Change_NameWarning"),
                    _command.Name,
                    flag.Value));
            _changeRequest.Name = flag.Value;
        }
        // Change command arg.
        else if (flag.Key == "path")
        {
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Change_PathWarning"),
                    _command.Name,
                    flag.Value));
            _changeRequest.Path = flag.Value;
        }
        // Change command type.
        else if (flag.Key == "type")
        {
            var commandDesc = _config.KnownCommands
                .FirstOrDefault(desc => desc.Selector == flag.Value);
            // Guard against unknown command type.
            if (commandDesc != null)
            {
                Formatter.DisplayWarning(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Change_TypeWarning"),
                        _command.Name,
                        commandDesc.Type));
                _changeRequest.Type = commandDesc.Type;
            }
            else
            {
                Formatter.DisplayError(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("FCli_UnknownCommandType"),
                        flag.Value));
                throw new FlagException(
                    "[Change] Unknown command type was specified.");
            }
        }
        // Change command shell.
        else if (flag.Key == "shell")
        {
            var shellDesc = _config.KnownShells
                .FirstOrDefault(desc => desc.Selector == flag.Value);
            // Guard against unknown shell type.
            if (shellDesc != null)
            {
                Formatter.DisplayWarning(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Change_ShellWarning"),
                        _command.Name,
                        shellDesc.Type));
                _changeRequest.Shell = shellDesc.Type;
            }
            else
            {
                Formatter.DisplayError(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("FCli_UnknownShell"),
                        flag.Value));
                throw new FlagException(
                    "[Change] Unknown shell type was specified.");
            }
        }
        // Change command options.
        else if (flag.Key == "options")
        {
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Change_OptionsWarning"),
                    _command.Name,
                    flag.Value));
            _changeRequest.Options = flag.Value;
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override Task ActionAsync()
    {
        // If no flags given display the state of the command.
        if (Flags.Count == 0)
        {
            Formatter.DisplayInfo(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Change_ShowCommand"),
                    _command.Name));
            Formatter.DisplayMessage($"Name    - {_command.Name}");
            Formatter.DisplayMessage($"Type    - {_command.Type}");
            if (_command.Type == CommandType.Group)
            {
                Formatter.DisplayMessage("Sequence:");
                Formatter.DisplayMessage(
                    $"\t{string.Join(' ', ((Group)_command).Sequence)}");
            }
            else
            {
                Formatter.DisplayMessage($"Path    - {_command.Path}");
                Formatter.DisplayMessage($"Shell   - {_command.Shell}");
                Formatter.DisplayMessage($"Options - {_command.Options}");
            }
            return Task.CompletedTask;
        }
        // Change command if flags were given.
        // Display new command profile
        Formatter.DisplayInfo(
            Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString("Change_NewCommandProfile"),
                _command.Name, _changeRequest.Name == ""
                    ? Resources.GetLocalizedString("Change_Same")
                    : _changeRequest.Name,
                _command.Path, _changeRequest.Path == ""
                    ? Resources.GetLocalizedString("Change_Same")
                    : _changeRequest.Path,
                _command.Type, _changeRequest.Type == CommandType.None
                    ? Resources.GetLocalizedString("Change_Same")
                    : _changeRequest.Type,
                _command.Shell, _changeRequest.Shell == ShellType.None
                    ? Resources.GetLocalizedString("Change_Same")
                    : _changeRequest.Shell,
                _command.Options, _changeRequest.Options == ""
                    ? Resources.GetLocalizedString("Change_Same")
                    : _changeRequest.Options));
        // Get user's confirmation.
        Formatter.DisplayWarning(
            Name,
            Resources.GetLocalizedString("Change_Warning"));
        if (!UserConfirm()) return Task.CompletedTask;
        // Replace old command with the new one command.
        Formatter.DisplayMessage(
            Resources.GetLocalizedString("FCli_Saving"));
        var command = _factory.Construct(_changeRequest);
        // Override.
        _loader.DeleteCommand(_command.Name);
        _loader.SaveCommand(command);
        Formatter.DisplayInfo(
            Name,
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.GetLocalizedString("FCli_CommandSaved"),
                command.Name));
        // Final.
        return Task.CompletedTask;
    }
}