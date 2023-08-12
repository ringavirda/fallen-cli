// FCli namespaces.
using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that lists all known selectors.
/// </summary>
public class ListTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandLoader _loader;

    public ListTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandLoader commandLoader)
        : base(formatter, resources)
    {
        _loader = commandLoader;
        _config = config;

        Description = resources.GetLocalizedString("List_Help");
    }

    //Private data.
    private List<Command> _commands = null!;

    // Overrides

    public override string Name => "List";
    public override string Description { get; }
    public override List<string> Selectors => new() { "list", "ls" };
    public override ToolType Type => ToolType.List;

    protected override void GuardInit()
    {
        // Attempt loading commands.
        var commands = _loader.LoadCommands();
        // Guard against empty command list.
        if (commands == null || !commands.Any())
        {
            _formatter.DisplayInfo(Name,
                _resources.GetLocalizedString("List_NoCommands"));
            return;
        }
        // Init private field.
        _commands = commands;
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // No List flags have values.
        FlagHasNoValue(flag, Name);

        // Load descriptor from the type flag.
        var commandDesc = _config.KnownCommands
            .FirstOrDefault(c => c.Selector == flag.Key);
        // No descriptors found.
        if (commandDesc != null)
        {
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("List_ListCommands"),
                commandDesc.Selector));
            var selected = _commands.Where(c => c.Type == commandDesc.Type);
            if (selected.Any())
                DisplayCommands(selected, Arg);
            else _formatter.DisplayMessage(string.Format(
                    _resources.GetLocalizedString(
                        "List_NoCommandsSelected"),
                    commandDesc.Selector));
        }
        // List all known tools.
        else if (flag.Key == "tools")
        {
            var tools = _config.KnownTools
                .Where(tool => tool.Name.Contains(Arg))
                .Select(tool =>
                    $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                        => $"{s1}, {s2}")}")
                        .Aggregate((s1, s2) => $"{s1}\n{s2}");
            DisplayString(Arg, tools);
        }
        // List all known shells.
        else if (flag.Key == "shells")
        {
            var shells = _config.KnownShells
                .Select(sh => sh.Selector)
                .Where(sh => sh.Contains(Arg))
                .Aggregate((s1, s2) => $"{s1}, {s2}");
            DisplayString(Arg, shells);
        }
        // List all known command types.
        else if (flag.Key == "types")
        {
            var types = _config.KnownCommands
                .Select(sh => sh.Selector)
                .Where(sh => sh.Contains(Arg))
                .Aggregate((s1, s2) => $"{s1}, {s2}");
            DisplayString(Arg, types);
        }
        // List all stored command groups.
        else if (flag.Key == "groups")
        {
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("List_ListCommands"),
                CommandType.Group));
            var selected = _commands
                .Where(command => command.Type == CommandType.Group);
            if (selected.Any())
                DisplayCommands(selected, Arg);
            else _formatter.DisplayMessage(string.Format(
                    _resources.GetLocalizedString(
                        "List_NoCommandsSelected"),
                    CommandType.Group));
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        // Display all commands if no flags were given.
        if (Flags.Count == 0)
        {
            _formatter.DisplayInfo(Name,
                _resources.GetLocalizedString("List_ListAllCommands"));
            DisplayCommands(_commands, Arg);
        }
    }

    // Private methods.

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
