// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

public class GroupTool : Tool
{
    // From ToolExecutor.
    private readonly ICommandLoader _loader;
    private readonly IToolExecutor _executor;
    private readonly ICommandFactory _factory;

    public GroupTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IToolExecutor executor,
        ICommandLoader loader,
        ICommandFactory factory)
        : base(formatter, resources)
    {
        _loader = loader;
        _executor = executor;
        _factory = factory;

        Description = resources.GetLocalizedString("Group_Help");
    }

    public override string Name => "Group";

    public override string Description { get; }

    public override List<string> Selectors => new() { "group", "gr" };

    public override ToolType Type => ToolType.Group;

    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against no arg.
            if (arg == "" && !flags.Any(flag => flag.Key == "all"))
            {
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetLocalizedString("FCli_ArgMissing"),
                    Name));
                throw new ArgumentException("Config attempt to call with arg");
            }
            bool skipConfirmation = false;
            if (flags.Any(flag => flag.Key == "yes"))
            {
                skipConfirmation = true;
            }
            // Parse args
            foreach (var flag in flags)
            {
                // Main creation case.
                if (flag.Key == "name")
                {
                    FlagHasValue(flag, Name);
                    // Guard against existing name.
                    NameIsFree(flag.Value);
                    // Make sure that all commands are present.
                    var commands = ValidateCommands(arg);
                    // Construct a command.
                    var group = _factory.ConstructGroup(flag.Value, commands);
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
                // Changes a group.
                else if (flag.Key == "override")
                {
                    FlagHasValue(flag, Name);
                    // Validate group.
                    var group = GroupExists(flag.Value);
                    // Validate new commands.
                    var commands = ValidateCommands(arg);
                    _formatter.DisplayWarning(Name, string.Format(
                        _resources.GetLocalizedString("Group_OverrideWarning"),
                        group.Name));
                    // Get user confirmation or skip.
                    if (!skipConfirmation && !UserConfirm())
                        return;
                    // Override.
                    _loader.DeleteCommand(group.Name);
                    group = _factory.ConstructGroup(group.Name, commands);
                    _loader.SaveCommand(group);
                    _formatter.DisplayInfo(Name, string.Format(
                        _resources.GetLocalizedString("FCli_CommandSaved"),
                        group.Name));

                }
                // Deletes existing group.
                else if (flag.Key == "remove")
                {
                    FlagHasNoValue(flag, Name);
                    if (flags.Any(f => f.Key == "all"))
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
                        var group = GroupExists(arg);
                        if (group.Type != CommandType.Group)
                        {
                            _formatter.DisplayError(Name, 
                                _resources.GetLocalizedString("Group_NotAGroup"));
                            throw new ArgumentException($"({arg}) wasn't a group.");
                        }
                        // Get user confirmation or skip.
                        if (!skipConfirmation && !UserConfirm())
                            return;
                        // Remove group.
                        _loader.DeleteCommand(group.Name);
                        _formatter.DisplayInfo(Name, string.Format(
                            _resources.GetLocalizedString("Group_Removed"),
                            group.Name));
                    }
                }
                else UnknownFlag(flag, Name);
            }
        };

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
                "Tried to override an unknown group.");
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
            || _executor.Tools.Any(tool => tool.Selectors.Contains(name)))
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_NameExists"),
                name));
            throw new CommandNameException(
                "Tried to create a group with existing name.");
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
                    "Tried to create a group with an unknown command.");
            }
        }
        return commands.ToList();
    }
}