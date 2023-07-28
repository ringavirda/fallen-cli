// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

public class ChangeTool : Tool
{
    // From ToolExecutor.
    private readonly ICommandLoader _loader;
    private readonly IToolExecutor _executor;
    private readonly ICommandFactory _factory;
    private readonly IConfig _config;

    public ChangeTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        IToolExecutor executor,
        ICommandLoader loader,
        ICommandFactory factory)
        : base(formatter, resources)
    {
        _loader = loader;
        _executor = executor;
        _factory = factory;
        _config = config;

        Description = resources.GetLocalizedString("Change_Help");
    }

    public override string Name => "Change";

    public override string Description { get; }

    public override List<string> Selectors => new() { "change", "ch" };

    public override ToolType Type => ToolType.Change;

    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against no arg.
            if (arg == "")
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetLocalizedString("FCli_ArgMissing"),
                    arg));
                throw new ArgumentException("Tried to change nothing.");
            }
            // Guard against invalid command name.
            var command = _loader.LoadCommand(arg);
            if (command == null)
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetLocalizedString("FCli_UnknownName"),
                    Name));
                throw new CommandNameException(
                    "Name specified in Change tool was invalid.");
            }
            // Forward declare.
            var name = string.Empty;
            var path = string.Empty;
            var options = string.Empty;
            var type = CommandType.None;
            var shell = ShellType.None;
            // Parse flags.
            foreach (var flag in flags)
            {
                // All Change flags have values.
                FlagHasValue(flag, Name);
                if (flag.Key == "name")
                {
                    if (_loader.CommandExists(flag.Value)
                        || _executor.Tools
                            .Any(tool => tool.Selectors.Contains(flag.Value)))
                    {
                        _formatter.DisplayError(Name, string.Format(
                            _resources.GetLocalizedString("FCli_NameExists"),
                            flag.Value));
                        throw new CommandNameException(
                            "Tried to create a command with existing name.");
                    }
                    _formatter.DisplayWarning(Name, string.Format(
                        _resources.GetLocalizedString("Change_NameWarning"),
                        command.Name, flag.Value));
                    name = flag.Value;
                }
                else if (flag.Key == "path")
                {
                    _formatter.DisplayWarning(Name, string.Format(
                        _resources.GetLocalizedString("Change_PathWarning"),
                        command.Name, flag.Value));
                    path = flag.Value;
                }
                else if (flag.Key == "type")
                {
                    var commandDesc = _config.KnownCommands
                        .FirstOrDefault(desc => desc.Selector == flag.Value);
                    // Guard against unknown command type.
                    if (commandDesc != null)
                    {
                        _formatter.DisplayWarning(Name, string.Format(
                            _resources.GetLocalizedString("Change_TypeWarning"),
                            command.Name, commandDesc.Type));
                        type = commandDesc.Type;
                    }
                    else
                    {
                        _formatter.DisplayError(Name, string.Format(
                            _resources.GetLocalizedString(
                                "FCli_UnknownCommandType"),
                                flag.Value));
                        throw new FlagException(
                            "Unknown command type was specified in Change tool.");
                    }
                }
                else if (flag.Key == "shell")
                {
                    var shellDesc = _config.KnownShells
                        .FirstOrDefault(desc => desc.Selector == flag.Value);
                    // Guard against unknown shell type.
                    if (shellDesc != null)
                    {
                        _formatter.DisplayWarning(Name, string.Format(
                            _resources.GetLocalizedString("Change_ShellWarning"),
                            command.Name, shellDesc.Type));
                        shell = shellDesc.Type;
                    }
                    else
                    {
                        _formatter.DisplayError(Name, string.Format(
                            _resources.GetLocalizedString("FCli_UnknownShell"),
                            flag.Value));
                        throw new FlagException(
                            "Unknown shell type was specified in Change tool.");
                    }
                }
                else if (flag.Key == "options")
                {
                    _formatter.DisplayWarning(Name, string.Format(
                        _resources.GetLocalizedString("Change_OptionsWarning"),
                        command.Name, flag.Value));
                    options = flag.Value;
                }
                else UnknownFlag(flag, Name);
            }
            // Guard against no change.
            if (name == "" && path == "" && type == CommandType.None
                && shell == ShellType.None && options == "")
            {
                _formatter.DisplayInfo(Name, 
                    _resources.GetLocalizedString("Change_NoChange"));
                return;
            }
            // Display new command profile
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("Change_NewCommandProfile"),
                command.Name, name == "" 
                    ? _resources.GetLocalizedString("Change_Same") : name,
                command.Path, path == "" 
                    ? _resources.GetLocalizedString("Change_Same") : path,
                command.Type, type == CommandType.None 
                    ? _resources.GetLocalizedString("Change_Same") : type,
                command.Shell, shell == ShellType.None 
                    ? _resources.GetLocalizedString("Change_Same") : shell,
                command.Options, options == "" 
                    ? _resources.GetLocalizedString("Change_Same") : options
            ));
            // Get user's confirmation.
            _formatter.DisplayWarning(Name, 
                _resources.GetLocalizedString("Change_Warning"));
            if (!UserConfirm()) return;
            // Save new command.
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("FCli_Saving"));
            var newCommand = _factory.Construct(
                name == "" ? command.Name : name,
                path == "" ? command.Path : path,
                type == CommandType.None ? command.Type : type,
                shell == ShellType.None ? command.Shell : shell,
                options == "" ? command.Options : options
            );
            _loader.DeleteCommand(command.Name);
            _loader.SaveCommand(newCommand);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("FCli_CommandSaved"),
                newCommand.Name));
        };
}