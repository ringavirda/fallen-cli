// Vendor namespaces.
using System.Resources;
using FCli.Models.Types;
// FCli namespaces.
using FCli.Services.Data;
using FCli.Services.Format;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

/// <summary>
/// A tool that removes commands from storage.
/// </summary>
public class RemoveTool : Tool
{
    // From ToolExecutor.
    private readonly ICommandLoader _loader;

    public RemoveTool(
        ICommandLineFormatter formatter,
        ResourceManager manager,
        ICommandLoader commandLoader)
        : base(formatter, manager)
    {
        _loader = commandLoader;

        Description = _resources.GetString("Remove_Help") 
            ?? formatter.StringNotLoaded();
    }

    public override string Name => "Remove";
    public override string Description { get; }
    public override List<string> Selectors => new() { "remove", "rm" };
    public override ToolType Type => ToolType.Remove;
    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against invalid command name.
            if (!_loader.CommandExists(arg)
                && !flags.Any(f => f.Key == "all"))
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetString("Remove_InvalidArg")
                    ?? _formatter.StringNotLoaded(),
                    arg
                ));
                throw new ArgumentException($"({arg}) - is not a command name.");
            }
            // Forward declare.
            bool skipDialog = false;
            // Parse flags.
            foreach (var flag in flags)
            {
                // No Remove flags have values.
                FlagHasNoValue(flag, Name);
                // Remove all flags.
                if (flag.Key == "all")
                {
                    // Confirm user's intentions.
                    _formatter.DisplayWarning(Name,
                        _resources.GetString("Remove_AllWarning"));
                    var response = _formatter.ReadUserInput("(yes/any)");
                    if (response?.ToLower() != "yes")
                        _formatter.DisplayMessage(
                            _resources.GetString("Remove_Averted"));
                    else
                    {
                        _formatter.DisplayMessage(
                            _resources.GetString("Remove_Deleting"));
                        var commands = _loader.LoadCommands();
                        // Guard against empty storage.
                        if (commands == null || !commands.Any())
                        {
                            _formatter.DisplayMessage(
                                _resources.GetString("Remove_NoCommands"));
                            return;
                        }
                        else
                        {
                            // Delete all known commands.
                            foreach (var command in commands
                                .Select(c => c.Name).ToList())
                                _loader.DeleteCommand(command);
                            _formatter.DisplayInfo(Name,
                                _resources.GetString("Remove_AllDeleted"));
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
            if (!skipDialog)
            {
                // Confirm user's intentions.
                _formatter.DisplayWarning(Name, string.Format(
                    _resources.GetString("Remove_Warning")
                    ?? _formatter.StringNotLoaded(),
                    arg
                ));
                var response = _formatter.ReadUserInput("(yes/any)");
                if (response != "yes")
                {
                    _formatter.DisplayMessage(
                        _resources.GetString("Remove_Averted"));
                    return;
                }
            }
            // Delete command.
            _formatter.DisplayMessage(
                _resources.GetString("Remove_Deleting"));
            _loader.DeleteCommand(arg);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetString("Remove_Deleted")
                ?? _formatter.StringNotLoaded(),
                arg
            ));
        };
}
