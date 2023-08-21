using System.Globalization;

using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that lists all known selectors.
/// </summary>
public class ListTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandLoader _loader;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public ListTool() : base()
    {
        _config = null!;
        _loader = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
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
    private List<Command>? _commands;

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
            Formatter.DisplayInfo(
                Name,
                Resources.GetLocalizedString("List_NoCommands"));
        }
        // Init private field.
        _commands = commands;
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Skip if no commands loaded.
        if (_commands == null) return;
        // No List flags have values.
        FlagHasNoValue(flag, Name);

        // Load descriptor from the type flag.
        var commandDesc = _config.KnownCommands
            .FirstOrDefault(c => c.Selector == flag.Key);
        // No descriptors found.
        if (commandDesc != null)
        {
            Formatter.DisplayInfo(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("List_ListCommands"),
                    commandDesc.Selector));
            // Extract typed commands.
            var selected = _commands.Where(c => c.Type == commandDesc.Type);
            if (selected.Any())
                DisplayCommands(selected, Arg);
            // Guard against no commands.
            else Formatter.DisplayMessage(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("List_NoCommandsSelected"),
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
            Formatter.DisplayInfo(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("List_ListCommands"),
                    CommandType.Group));
            // Extract commands.
            var selected = _commands
                .Where(command => command.Type == CommandType.Group);
            if (selected.Any())
                DisplayCommands(selected, Arg);
            // Guard against no groups.
            else Formatter.DisplayMessage(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("List_NoCommandsSelected"),
                    CommandType.Group));
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override Task ActionAsync()
    {
        // Skip if no commands loaded.
        if (_commands == null) return Task.CompletedTask;
        // Display all commands if no flags were given.
        if (Flags.Count == 0)
        {
            Formatter.DisplayInfo(
                Name,
                Resources.GetLocalizedString("List_ListAllCommands"));
            DisplayCommands(_commands, Arg);
        }
        // Final.
        return Task.CompletedTask;
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
            Formatter.DisplayMessage(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("List_NothingFiltered"),
                    arg));
        else Formatter.DisplayMessage(conf);
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
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("List_NothingFiltered"),
                        filter));
                return;
            }
        }
        foreach (var command in commands)
        {
            Formatter.DisplayMessage($"[{command.Type}] - {command.Name}:");
            if (command.Type == CommandType.Group)
                Formatter.DisplayMessage(
                    '\t' + string.Join(' ', ((Group)command).Sequence));
            else Formatter.DisplayMessage('\t' + command.Path);
        }
    }
}