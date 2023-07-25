// FCli namespaces.
using FCli.Services;
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

    public ListTool(
        ICommandLineFormatter formatter,
        IToolExecutor toolExecutor,
        ICommandLoader commandLoader)
        : base(formatter)
    {
        _executor = toolExecutor;
        _loader = commandLoader;
    }

    public override string Name => "List";
    public override string Description => """
        List - echos existing commands to the console based on the selection
        given by flags. If no flags given - lists all existing commands.
        Flags:
            --script - adds scripts to listing.
            --exe    - adds executables to listing.
            --url    - adds urls to listing.
            --tools  - lists all available tool selectors.
            --help   - show description.
        Usage:
            fcli list
            fcli list --tools
            fcli ls --script --url 
        """;
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
                _formatter.DisplayInfo(Name, "There are no known commands.");
                return;
            }
            // Display all commands if no flags are given.
            if (flags.Count == 0)
            {
                _formatter.DisplayInfo(
                    Name,
                    "No flags given, listing all commands:");
                DisplayCommands(commands, arg);
                return;
            }
            // Parse flags.
            foreach (var flag in flags)
            {
                // No LIST flags have values.
                FlagHasNoValue(flag, Name);
                // Display all scripts.
                if (flag.Key == "script")
                {
                    var scripts = commands.Where(command =>
                        command.Type == CommandType.CMD
                        || command.Type == CommandType.Powershell
                        || command.Type == CommandType.Bash);
                    if (scripts.Any())
                    {
                        _formatter.DisplayInfo(Name, "Listing all scripts:");
                        DisplayCommands(scripts, arg);
                    }
                    else _formatter.DisplayInfo(Name, "There are no known scripts.");
                }
                // List all websites.
                else if (flag.Key == "url")
                {
                    var urls = commands.Where(command =>
                        command.Type == CommandType.Url);
                    if (urls.Any())
                    {
                        _formatter.DisplayInfo(Name, "Listing all urls:");
                        DisplayCommands(urls, arg);
                    }
                    else _formatter.DisplayInfo(Name, "There are no known urls.");
                }
                // List all known executables.
                else if (flag.Key == "exe")
                {
                    var executables = commands.Where(command =>
                        command.Type == CommandType.Executable);
                    if (executables.Any())
                    {
                        _formatter.DisplayInfo(Name, "Listing all executables:");
                        DisplayCommands(executables, arg);
                    }
                    else _formatter.DisplayInfo(Name, "There are no known executables.");
                }
                // List all known tools.
                else if (flag.Key == "tool")
                {
                    if (arg != "")
                    {
                        _formatter.DisplayWarning(
                            Name,
                            "(--tool) cannot be used with a filer.");
                        throw new ArgumentException("--tool was called with arg.");
                    }
                    var allTools = _executor.KnownTools
                        .Select(tool =>
                            $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                                => $"{s1}, {s2}")}")
                                .Aggregate((s1, s2) => $"{s1}\n{s2}");

                    _formatter.DisplayInfo(Name, "All known tool selectors:");
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
                _formatter.DisplayMessage(
                    $"No commands were found with given filter: {filter}");
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
