// FCli namespaces.
using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

public class GroupTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly ICommandLoader _loader;
    private readonly ICommandFactory _factory;

    public GroupTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        ICommandLoader loader,
        ICommandFactory factory)
        : base(formatter, resources)
    {
        _config = config;
        _loader = loader;
        _factory = factory;

        Description = resources.GetLocalizedString("Group_Help");
    }

    // Private data.
    private bool _skipConfirm = false;
    private GroupAlterRequest _alterRequest = new();

    // Overrides.

    public override string Name => "Group";
    public override string Description { get; }
    public override List<string> Selectors => new() { "group", "gr" };
    public override ToolType Type => ToolType.Group;

    protected override void GuardInit()
    {
        // Guard against no arg.
        if (Arg == "" && !Flags.Any(flag => flag.Key == "all"))
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_ArgMissing"),
                Name));
            throw new ArgumentException("[Group] No arg was given.");
        }
        if (Flags.Any(flag => flag.Key == "yes"))
            _skipConfirm = true;
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Create new group.
        if (flag.Key == "name")
        {
            FlagHasValue(flag, Name);
            _alterRequest.Name = flag.Value;
            // Guard against existing name.
            NameIsFree(flag.Value);
            // Make sure that all commands are present.
            _alterRequest.Sequence = ValidateCommands(Arg);
            // Construct a command.
            var group = _factory.ConstructGroup(_alterRequest);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("Group_Constructed"),
                group.Name, $"({string.Join(" ", group.Sequence)})"));
            // Save it.
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("FCli_Saving"));
            _loader.SaveCommand(group);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("FCli_CommandSaved"),
                group.Name));
        }
        // Change given group.
        else if (flag.Key == "override")
        {
            FlagHasValue(flag, Name);
            // Validate group.
            var group = GroupExists(flag.Value);
            _alterRequest = group.ToAlterRequest();
            // Validate new commands.
            _alterRequest.Sequence = ValidateCommands(Arg);
            _formatter.DisplayWarning(Name, string.Format(
                _resources.GetLocalizedString("Group_OverrideWarning"),
                group.Name));
            // Get user confirmation or skip.
            if (!_skipConfirm && !UserConfirm())
                return;
            // Override.
            _loader.DeleteCommand(group.Name);
            group = _factory.ConstructGroup(_alterRequest);
            _loader.SaveCommand(group);
            _formatter.DisplayInfo(Name, string.Format(
                _resources.GetLocalizedString("FCli_CommandSaved"),
                group.Name));

        }
        // Delete existing group.
        else if (flag.Key == "remove")
        {
            FlagHasNoValue(flag, Name);
            if (Flags.Any(f => f.Key == "all"))
            {
                _formatter.DisplayWarning(Name,
                    _resources.GetLocalizedString("Group_RemoveAllWarning"));
                // Get user confirmation, no skipping.
                if (!UserConfirm()) return;
                // Remove all.
                var groups = _loader.LoadCommands()
                    ?.Where(command => command.Type == CommandType.Group);
                if (groups == null || !groups.Any())
                {
                    _formatter.DisplayInfo(Name,
                        _resources.GetLocalizedString("Group_NoGroups"));
                    return;
                }
                else
                {
                    // Delete all.
                    foreach (var group in groups)
                        _loader.DeleteCommand(group.Name);
                    _formatter.DisplayInfo(Name,
                        _resources.GetLocalizedString("Group_RemovedAll"));
                }
            }
            else
            {
                // Confirm group exists.
                var group = GroupExists(Arg);
                if (group.Type != CommandType.Group)
                {
                    _formatter.DisplayError(Name,
                        _resources.GetLocalizedString("Group_NotAGroup"));
                    throw new ArgumentException($"[Group] ({Arg}) wasn't a group.");
                }
                // Get user confirmation or skip.
                if (!_skipConfirm && !UserConfirm())
                    return;
                // Remove group.
                _loader.DeleteCommand(group.Name);
                _formatter.DisplayInfo(Name, string.Format(
                    _resources.GetLocalizedString("Group_Removed"),
                    group.Name));
            }
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        // Group tool is entirely flag based.
    }

    // Private methods.

    /// <summary>
    /// Makes sure that specified group exists and returns it loaded.
    /// </summary>
    /// <param name="name">Group name.</param>
    /// <returns>Loaded group.</returns>
    /// <exception cref="FlagException">If </exception>
    private Group GroupExists(string name)
    {
        var group = (Group?)_loader.LoadCommand(name);
        if (group == null)
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_UnknownName"),
                name));
            throw new CommandNameException(
                "[Group] Tried to override an unknown group.");
        }
        return group;
    }

    /// <summary>
    /// Checks if specified name is present amongst commands and tools.
    /// </summary>
    /// <param name="name">Group to check.</param>
    /// <exception cref="CommandNameException">If name already exists.</exception>
    private void NameIsFree(string name)
    {
        if (_loader.CommandExists(name)
            || _config.KnownTools.Any(tool => tool.Selectors.Contains(name)))
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_NameExists"),
                name));
            throw new CommandNameException(
                "[Group] Tried to create a group with existing name.");
        }
    }

    /// <summary>
    /// Parses command sequence and validates each of them.
    /// </summary>
    /// <param name="arg">Commands, separated by spaces.</param>
    /// <returns>List of loaded commands.</returns>
    /// <exception cref="ArgumentException">If command is unknown.</exception>
    private List<string> ValidateCommands(string arg)
    {
        var commands = arg.Split(' ');
        foreach (var name in commands)
        {
            if (!_loader.CommandExists(name))
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetLocalizedString("FCli_UnknownName"),
                    name));
                throw new CommandNameException(
                    "[Group] Tried to create a group with an unknown command.");
            }
        }
        return commands.ToList();
    }
}
