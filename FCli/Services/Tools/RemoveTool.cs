// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that removes commands from storage.
/// </summary>
public class RemoveTool : ToolBase
{
    // DI.
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

    // Private data.
    private bool _skipConfirm = false;
    private bool _skipAction = false;

    // Overrides.

    public override string Name => "Remove";
    public override string Description { get; }
    public override List<string> Selectors => new() { "remove", "rm" };
    public override ToolType Type => ToolType.Remove;

    protected override void GuardInit()
    {
        // Guard against invalid command name.
        if (!_loader.CommandExists(Arg)
            && !Flags.Any(f => f.Key == "all"))
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_UnknownName"),
                Arg
            ));
            throw new CommandNameException($"({Arg}) - is not a command name.");
        }
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // No Remove flags have values.
        FlagHasNoValue(flag, Name);

        // Remove all flag.
        if (flag.Key == "all")
        {
            // Confirm user's intentions.
            _formatter.DisplayWarning(Name,
                _resources.GetLocalizedString("Remove_AllWarning"));
            if (!UserConfirm()) _skipAction = true;
            // Delete all.
            var commands = _loader.LoadCommands();
            // Guard against empty storage.
            if (commands == null || !commands.Any())
            {
                _formatter.DisplayMessage(
                    _resources.GetLocalizedString("Remove_NoCommands"));
                _skipAction = true;
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
            _skipAction = true;
        }
        // Skip confirmation dialog.
        else if (flag.Key == "yes") _skipConfirm = true;
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        if (!_skipAction)
        {
            // Prepare to delete the command.
            // Confirm user's intentions.
            _formatter.DisplayWarning(Name, string.Format(
                _resources.GetLocalizedString("Remove_Warning"),
                Arg));
            if (!_skipConfirm && !UserConfirm()) return;
            // Delete command.
            _loader.DeleteCommand(Arg);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("Remove_Deleted"),
                Arg));
        }
    }
}
