// FCli namespaces.
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that lists all known selectors.
/// </summary>
public class ListTool : Tool
{
    // From ToolExecutor.
    private readonly IToolExecutor _executor;
    private readonly ICommandLoader _loader;
    private readonly IConfig _config;

    public ListTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        IToolExecutor toolExecutor,
        ICommandLoader commandLoader)
        : base(formatter, resources)
    {
        _executor = toolExecutor;
        _loader = commandLoader;
        _config = config;

        Description = resources.GetLocalizedString("List_Help");
    }

    public override string Name => "List";
    public override string Description { get; }
    public override List<string> Selectors => new() { "list", "ls" };
    public override ToolType Type => ToolType.List;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }

            // Attempt loading commands.
            var commands = _loader.LoadCommands();
            // Guard against empty command list.
            if (commands == null || !commands.Any())
            {
                _formatter.DisplayInfo(Name,
                    _resources.GetLocalizedString("List_NoCommands"));
                return;
            }
            // Display all commands if no flags are given.
            if (flags.Count == 0)
            {
                _formatter.DisplayInfo(Name,
                    _resources.GetLocalizedString("List_ListAllCommands"));
                DisplayCommands(commands, arg);
                return;
            }
            // Parse flags.
            foreach (var flag in flags)
            {
                // No List flags have values.
                FlagHasNoValue(flag, Name);
                // Display all scripts.
                var commandDesc = _config.KnownCommands
                    .FirstOrDefault(c => c.Selector == flag.Key);
                if (commandDesc != null)
                {
                    _formatter.DisplayInfo(Name, string.Format(
                        _resources.GetLocalizedString("List_ListCommands"),
                        commandDesc.Selector));
                    var selected = commands.Where(c => c.Type == commandDesc.Type);
                    if (selected.Any())
                        DisplayCommands(selected, arg);
                    else _formatter.DisplayMessage(string.Format(
                            _resources.GetLocalizedString(
                                "List_NoCommandsSelected"),
                            commandDesc.Selector));
                }
                // List all known tools.
                else if (flag.Key == "tools")
                {
                    var tools = _executor.Tools
                        .Where(tool => tool.Name.Contains(arg))
                        .Select(tool =>
                            $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                                => $"{s1}, {s2}")}")
                                .Aggregate((s1, s2) => $"{s1}\n{s2}");
                    DisplayString(arg, tools);
                }
                else if (flag.Key == "shells")
                {
                    var shells = _config.KnownShells
                        .Select(sh => sh.Selector)
                        .Where(sh => sh.Contains(arg))
                        .Aggregate((s1, s2) => $"{s1}, {s2}");
                    DisplayString(arg, shells);
                }
                else if (flag.Key == "types")
                {
                    var types = _config.KnownCommands
                        .Select(sh => sh.Selector)
                        .Where(sh => sh.Contains(arg))
                        .Aggregate((s1, s2) => $"{s1}, {s2}");
                    DisplayString(arg, types);
                }
                else if (flag.Key == "groups")
                {
                    _formatter.DisplayInfo(Name, string.Format(
                        _resources.GetLocalizedString("List_ListCommands"),
                        CommandType.Group));
                    var selected = commands
                        .Where(command => command.Type == CommandType.Group);
                    if (selected.Any())
                        DisplayCommands(selected, arg);
                    else _formatter.DisplayMessage(string.Format(
                            _resources.GetLocalizedString(
                                "List_NoCommandsSelected"),
                            CommandType.Group));
                }
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
        };

    /// <summary>
    /// Checks if string is not null and then prints it out.
    /// </summary>
    /// <param name="arg">Filter.</param>
    /// <param name="conf">String to print.</param>
    private void DisplayString(string arg, string conf)
    {
        if (conf == string.Empty)
            _formatter.DisplayMessage(string.Format(
                _resources.GetLocalizedString("List_NothingFiltered"),
                arg));
        else _formatter.DisplayMessage(conf);
    }

    /// <summary>
    /// Prints to console an enumerable of Command in a formatted way.
    /// </summary>
    /// <param name="commands">Commands to print out.</param>
    private void DisplayCommands(
        IEnumerable<Command> commands,
        string filter)
    {
        if (filter != "")
        {
            commands = commands.Where(command => command.Name.Contains(filter));
            if (!commands.Any())
            {
                _formatter.DisplayMessage(string.Format(
                    _resources.GetLocalizedString("List_NothingFiltered"),
                    filter));
                return;
            }
        }
        foreach (var command in commands)
        {
            _formatter.DisplayMessage($"[{command.Type}] - {command.Name}:");
            if (command.Type == CommandType.Group)
                _formatter.DisplayMessage(
                    $"\t{string.Join(' ', ((Group)command).Sequence)}");
            else _formatter.DisplayMessage($"\t{command.Path}");
        }
    }
}
