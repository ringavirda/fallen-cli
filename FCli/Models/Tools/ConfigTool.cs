using System.Resources;
using FCli.Exceptions;
using FCli.Services;
using FCli.Services.Format;
using static FCli.Models.Args;

namespace FCli.Models.Tools;

public class ConfigTool : Tool
{
    // From ToolExecutor.
    private readonly IConfig _config;

    public ConfigTool(
        ICommandLineFormatter formatter,
        ResourceManager manager,
        IConfig config)
        : base(formatter, manager)
    {
        _config = config;

        Description = _resources.GetString("ConfigHelp") 
            ?? "Description hasn't loaded";
    }

    public override string Name => "Config";

    public override string Description { get; }

    public override List<string> Selectors => new() { "config", "cnf" };

    public override ToolType Type => ToolType.Config;

    public override Action<string, List<Flag>> Action =>
        (string arg, List<Flag> flags) =>
        {
            // Handle --help flag.
            if (flags.Any(flag => flag.Key == "help"))
            {
                _formatter.DisplayMessage(Description);
                return;
            }
            // Guard against arg.
            if (arg != "")
            {
                _formatter.DisplayWarning(Name, """
                    Config tool cannot have any arguments.
                    For Config tool syntax consult help page using --help.
                    """);
                throw new ArgumentException("Config attempt to call with arg");
            }
            // If no flags display config state.
            if (!flags.Any())
            {
                _formatter.DisplayInfo(Name, "No flags given, listing current config:");
                // Temporary hardcode.
                _formatter.DisplayMessage($"Formatter: {_config.Formatter}");
                _formatter.DisplayMessage($"Locale: {_config.Locale}");
            }
            foreach (var flag in flags)
            {
                if (flag.Key == "locale")
                {
                    FlagHasValue(flag, Name);
                    // Guard against unsupported locale.
                    if (!_config.KnownLocales.Contains(flag.Value))
                    {
                        _formatter.DisplayError(Name, """
                            Unsupported locale was specified via --locale flag.
                            To see supported locales consult help page.
                            """);
                        throw new FlagException(
                            $"Unsupported locale ({flag.Value}) was specified.");
                    }
                    _formatter.DisplayWarning(
                        Name,
                        $"Preparing to change locale from ({_config.Locale}) to ({flag.Value})...");
                    _config.ChangeLocale(flag.Value);
                    _formatter.DisplayMessage("Locale changed.");
                }
                else if (flag.Key == "formatter")
                {
                    FlagHasValue(flag, Name);
                    // Guard against unsupported locale.
                    if (!_config.KnownFormatters.ContainsKey(flag.Value))
                    {
                        _formatter.DisplayError(Name, """
                            Unsupported formatter was specified via --formatter flag.
                            To see supported console formatters consult help page.
                            """);
                        throw new FlagException(
                            $"Unsupported formatter ({flag.Value}) was specified.");
                    }
                    _formatter.DisplayWarning(
                        Name,
                        $"Preparing to change command line formatter from ({_config.Formatter}) to ({flag.Value})...");
                    _config.ChangeFormatter(flag.Value);
                    _formatter.DisplayMessage("Formatter changed.");
                }
                else if (flag.Key == "purge")
                {
                    FlagHasNoValue(flag, Name);
                    // Require confirmation from user.
                    _formatter.DisplayWarning(Name, """
                        You are about to purge your dynamic configuration, all your
                        setups will be lost. Are you sure you want to proceed?
                        """);
                    var response = _formatter.ReadUserInput("(yes/any)");
                    if (response != "yes")
                    {
                        _formatter.DisplayMessage("Config purge averted.");
                        return;
                    }
                    _formatter.DisplayMessage("Purging...");
                    _config.PurgeConfig();
                    _formatter.DisplayMessage("Purged.");
                }
                else UnknownFlag(flag, Name);
            }
        };
}