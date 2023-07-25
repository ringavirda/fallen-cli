// FCli namespaces.
using FCli.Common;
using FCli.Services.Data;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that removes commands from storage.
/// </summary>
public class RemoveTool : Tool
{
    // From ToolExecutor.
    private readonly ICommandLoader _commandLoader;

    public RemoveTool(ICommandLoader commandLoader)
    {
        _commandLoader = commandLoader;
    }

    public override string Name => "Remove";
    public override string Description => """
            Remove - deletes command from storage.
            Flags:
                --yes  - skip confirmation.
                --all  - removes all known commands.
                --help - show description.
            Usage:
                fcli remove awesome --yes
            """;
    public override List<string> Selectors => new() { "remove", "rm" };
    public override ToolType Type => ToolType.Remove;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                Helpers.DisplayInfo(Name, Description);
                return;
            }
            // Guard against invalid command name.
            if (!_commandLoader.CommandExists(arg)
                && !flags.Any(f => f.Key == "all"))
            {
                Helpers.DisplayError(Name, $"""
                    ({arg}) - is not a recognized command name.
                    To see all command selectors try: fcli list.
                    """);
                throw new ArgumentException($"({arg}) - is not a command name.");
            }
            // Forward declare.
            bool skipDialog = false;
            // Parse flags.
            foreach (var flag in flags)
            {
                // No REMOVE flags have values.
                FlagHasNoValue(flag, Name);
                // Remove all flags.
                if (flag.Key == "all")
                {
                    // Confirm user's intentions.
                    Helpers.DisplayWarning(
                        Name,
                        "All flag: preparing to delete all known commands.");
                    Helpers.DisplayMessage("Are you sure? (yes/any): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "yes")
                        Helpers.DisplayMessage("Deletion averted.");
                    else
                    {
                        Helpers.DisplayMessage("Deleting...");
                        var commands = _commandLoader.LoadCommands();
                        // Guard against empty storage.
                        if (commands == null || !commands.Any())
                        {
                            Helpers.DisplayError(
                                Name,
                                "There are no commands to delete!");
                            return;
                        }
                        else
                        {
                            // Delete all known commands.
                            foreach (var command in commands
                                .Select(c => c.Name).ToList())
                                _commandLoader.DeleteCommand(command);
                            Helpers.DisplayInfo(
                                Name,
                                "All existing commands have been deleted.");
                        }
                    }
                    return;
                }
                // Skip confirmation dialog.
                if (flag.Key == "yes") skipDialog = true;
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
            // Prepare to delete the command.
            Helpers.DisplayInfo(Name, $"Preparing to delete {arg} command.");
            if (!skipDialog)
            {
                // Confirm user's intentions.
                Helpers.DisplayMessage("Are you sure? (yes/any): ");
                var response = Console.ReadLine();
                if (response != "yes")
                {
                    Helpers.DisplayMessage("Deletion averted.");
                    return;
                }
            }
            // Delete command.
            Helpers.DisplayMessage("Deleting...");
            _commandLoader.DeleteCommand(arg);
            Helpers.DisplayInfo(Name, $"Command ({arg}) was successfully deleted.");
        };
}