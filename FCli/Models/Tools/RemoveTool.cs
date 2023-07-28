// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
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
        IResources resources,
        ICommandLoader commandLoader)
        : base(formatter, resources)
    {
        _loader = commandLoader;

        Description = resources.GetLocalizedString("Remove_Help");
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
                    _resources.GetLocalizedString("FCli_UnknownName"),
                    arg
                ));
                throw new CommandNameException($"({arg}) - is not a command name.");
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
                        _resources.GetLocalizedString("Remove_AllWarning"));
                    if (!UserConfirm()) return;
                    // Delete all.
                    var commands = _loader.LoadCommands();
                    // Guard against empty storage.
                    if (commands == null || !commands.Any())
                    {
                        _formatter.DisplayMessage(
                            _resources.GetLocalizedString("Remove_NoCommands"));
                        return;
                    }
                    else
                    {
                        // Delete all known commands.
                        foreach (var command in commands
                            .Select(c => c.Name).ToList())
                            _loader.DeleteCommand(command);
                        _formatter.DisplayInfo(Name,
                            _resources.GetLocalizedString("Remove_AllDeleted"));
                    }
                    return;
                }
                // Skip confirmation dialog.
                if (flag.Key == "yes") skipDialog = true;
                // Throw if flag is unrecognized.
                else UnknownFlag(flag, Name);
            }
            // Prepare to delete the command.
            // Confirm user's intentions.
            _formatter.DisplayWarning(Name, string.Format(
                _resources.GetLocalizedString("Remove_Warning"),
                arg));
            if (!skipDialog && !UserConfirm()) return;
            // Delete command.
            _loader.DeleteCommand(arg);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("Remove_Deleted"),
                arg));
        };
}
