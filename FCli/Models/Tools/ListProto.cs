// FCli namespaces.
using FCli.Common;
using FCli.Services;
using FCli.Services.Data;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// Prototype for the tool that lists known selectors.
/// </summary>
public class ListProto : Tool, IToolProto
{
    // From ToolExecutor.
    private readonly IToolExecutor _toolExecutor;
    private readonly ICommandLoader _commandLoader;

    public ListProto(
        IToolExecutor toolExecutor,
        ICommandLoader commandLoader)
    {
        Name = "List";
        Description = """
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
        Type = ToolType.List;
        Selectors = new() { "list", "ls" };

        _toolExecutor = toolExecutor;
        _commandLoader = commandLoader;
    }

    /// <summary>
    /// Construct a LIST tool from this prototype.
    /// </summary>
    /// <returns>Constructed LIST tool.</returns>
    public Tool GetTool()
    {
        // Begin LIST logic construction.
        Action = (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                Helpers.DisplayInfo(Name, Description);
                return;
            }

            // Attempt loading commands.
            var commands = _commandLoader.LoadCommands();
            // Guard against empty command list.
            if (commands == null || !commands.Any())
            {
                Helpers.DisplayInfo(Name, "There are no known commands.");
                return;
            }
            // Display all commands if no flags are given.
            if (flags.Count == 0)
            {
                Helpers.DisplayInfo(
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
                        Helpers.DisplayInfo(Name, "Listing all scripts:");
                        DisplayCommands(scripts, arg);
                    }
                    else Helpers.DisplayInfo(Name, "There are no known scripts.");
                }
                // List all websites.
                else if (flag.Key == "url")
                {
                    var urls = commands.Where(command =>
                        command.Type == CommandType.Url);
                    if (urls.Any())
                    {
                        Helpers.DisplayInfo(Name, "Listing all urls:");
                        DisplayCommands(urls, arg);
                    }
                    else Helpers.DisplayInfo(Name, "There are no known urls.");
                }
                // List all known executables.
                else if (flag.Key == "exe")
                {
                    var executables = commands.Where(command =>
                        command.Type == CommandType.Executable);
                    if (executables.Any())
                    {
                        Helpers.DisplayInfo(Name, "Listing all executables:");
                        DisplayCommands(executables, arg);
                    }
                    else Helpers.DisplayInfo(Name, "There are no known executables.");
                }
                // List all known tools.
                else if (flag.Key == "tool")
                {
                    if (arg != "")
                    {
                        Helpers.DisplayWarning(
                            Name,
                            "(--tool) cannot be used with a filer.");
                        throw new ArgumentException("--tool was called with arg.");
                    }
                    var allTools = _toolExecutor.ToolProtos
                        .Select(proto => (Tool)proto)
                            .Select(tool =>
                                $"{tool.Name}: {tool.Selectors.Aggregate((s1, s2)
                                    => $"{s1}, {s2}")}")
                                    .Aggregate((s1, s2) => $"{s1}\n{s2}");

                    Helpers.DisplayInfo(Name, "All known tool selectors:");
                    Helpers.DisplayMessage(allTools);
                }
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
        };
        // Return constructed LIST tool.
        return this;
    }

    /// <summary>
    /// Prints to console an enumerable of Command in a formatted way.
    /// </summary>
    /// <param name="commands">Commands to print out.</param>
    private static void DisplayCommands(
        IEnumerable<Command> commands,
        string filter)
    {
        if (filter != "")
        {
            commands = commands.Where(command => command.Name.Contains(filter));
            if (!commands.Any())
            {
                Helpers.DisplayMessage(
                    $"No commands were found with given filter: {filter}");
                return;
            }
        }
        foreach (var command in commands)
        {
            Helpers.DisplayMessage($"[{command.Type}] - {command.Name}:");
            Helpers.DisplayMessage($"\t{command.Path}");
        }
    }
}