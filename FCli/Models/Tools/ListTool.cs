// Vendor namespaces.
using System.Resources;
// FCli namespaces.
using FCli.Models.Types;
using FCli.Services;
using FCli.Services.Config;
using FCli.Services.Data;
using FCli.Services.Format;
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
        ResourceManager manager,
        IToolExecutor toolExecutor,
        ICommandLoader commandLoader,
        IConfig config)
        : base(formatter, manager)
    {
        _executor = toolExecutor;
        _loader = commandLoader;
        _config = config;

        Description = _resources.GetString("List_Help") 
            ?? formatter.StringNotLoaded();
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
                    _resources.GetString("List_NoCommands"));
                return;
            }
            // Display all commands if no flags are given.
            if (flags.Count == 0)
            {
                _formatter.DisplayInfo(Name,
                    _resources.GetString("List_ListAllCommands"));
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
                        _resources.GetString("List_ListCommands")
                        ?? _formatter.StringNotLoaded(),
                        commandDesc.Selector));
                    var selected = commands.Where(c => c.Type == commandDesc.Type);
                    if (selected.Any())
                        DisplayCommands(selected, arg);
                    else _formatter.DisplayMessage(string.Format(
                            _resources.GetString("List_NoCommandsSelected")
                            ?? _formatter.StringNotLoaded(),
                            commandDesc.Selector));
                }
                // List all known tools.
                else if (flag.Key == "tool")
                {
                    if (arg != "")
                    {
                        _formatter.DisplayWarning(Name,
                            _resources.GetString("List_ToolArg"));
                        throw new ArgumentException("--tool was called with an arg.");
                    }
                    var allTools = _executor.Tools
                        .Select(tool =>
                            $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                                => $"{s1}, {s2}")}")
                                .Aggregate((s1, s2) => $"{s1}\n{s2}");

                    _formatter.DisplayInfo(Name,
                        _resources.GetString("List_Tools"));
                    _formatter.DisplayMessage(allTools);
                }
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
        };

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
                    _resources.GetString("List_NothingFiltered") 
                    ?? _formatter.StringNotLoaded(), 
                    filter));
                return;
            }
        }
        foreach (var command in commands)
        {
            _formatter.DisplayMessage($"[{command.Type}] - {command.Name}:");
            _formatter.DisplayMessage($"\t{command.Path}");
        }
    }
}
