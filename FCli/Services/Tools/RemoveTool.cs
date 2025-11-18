using System.Globalization;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

/// <summary>
/// A tool that removes commands from storage.
/// </summary>
public class RemoveTool : ToolBase
{
    // DI.
    private readonly ICommandLoader _loader;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public RemoveTool() : base()
    {
        _loader = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
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
    private bool _skipConfirm;
    private bool _skipAction;

    // Overrides.

    public override string Name => "Remove";
    public override string Description { get; }
    public override List<string> Selectors => new() { "remove", "rm" };
    public override ToolType Type => ToolType.Remove;

    protected override void GuardInit()
    {
        // Guard against invalid command name.
        if (!_loader.CommandExists(Arg) && !Flags.Any(f => f.Key == "all"))
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_UnknownName"),
                    Arg));
            throw new CommandNameException(
                $"[Remove] ({Arg}) is not a known command name.");
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
            Formatter.DisplayWarning(Name,
                Resources.GetLocalizedString("Remove_AllWarning"));
            if (!UserConfirm())
            {
                _skipAction = true;
                return;
            }
            // Delete all.
            var commands = _loader.LoadCommands();
            // Guard against empty storage.
            if (commands == null || !commands.Any())
            {
                Formatter.DisplayMessage(
                    Resources.GetLocalizedString("Remove_NoCommands"));
                _skipAction = true;
            }
            else
            {
                // Delete all known commands.
                foreach (var command in commands.Select(c => c.Name))
                    _loader.DeleteCommand(command);
                Formatter.DisplayInfo(
                    Name,
                    Resources.GetLocalizedString("Remove_AllDeleted"));
            }
            _skipAction = true;
        }
        // Skip confirmation dialog.
        else if (flag.Key == "yes") _skipConfirm = true;
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override Task ActionAsync()
    {
        if (!_skipAction)
        {
            // Prepare to delete the command.
            // Confirm user's intentions.
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Remove_Warning"),
                    Arg));
            if (!_skipConfirm && !UserConfirm()) return Task.CompletedTask;
            // Delete command.
            _loader.DeleteCommand(Arg);
            Formatter.DisplayInfo(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Remove_Deleted"),
                    Arg));
        }
        // Final
        return Task.CompletedTask;
    }
}