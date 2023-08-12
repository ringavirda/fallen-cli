// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

public class ConfigTool : ToolBase
{
    // DI.
    private readonly IConfig _config;

    public ConfigTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config)
        : base(formatter, resources)
    {
        _config = config;

        Description = resources.GetLocalizedString("Config_Help");
    }

    // Overrides.

    public override string Name => "Config";
    public override string Description { get; }
    public override List<string> Selectors => new() { "config", "cnf" };
    public override ToolType Type => ToolType.Config;

    protected override void GuardInit()
    {
        // Guard against arg.
        if (Arg != string.Empty)
        {
            _formatter.DisplayError(Name, string.Format(
                _resources.GetLocalizedString("FCli_UnexpectedArg"),
                Name));
            throw new ArgumentException("[Config] Unexpected arg.");
        }
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Change current locale.
        if (flag.Key == "locale")
        {
            FlagHasValue(flag, Name);
            // Guard against unsupported locale.
            if (!_config.KnownLocales.Contains(flag.Value))
            {
                _formatter.DisplayError(Name,
                    _resources.GetLocalizedString("Config_UnknownLocale"));
                throw new FlagException(
                    $"[Config] Unsupported locale ({flag.Value}) was specified.");
            }
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString(
                        "Config_LocaleChangeWarning"),
                        _config.Locale, flag.Value
                ));
            _config.ChangeLocale(flag.Value);
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("Config_LocaleChanged"));
        }
        // Change current command line formatter.
        else if (flag.Key == "formatter")
        {
            FlagHasValue(flag, Name);
            var newFormatter = _config.KnownFormatters
                .FirstOrDefault(format => format.Selector == flag.Value);
            // Guard against unsupported locale.
            if (newFormatter == null)
            {
                _formatter.DisplayError(Name,
                    _resources.GetLocalizedString(
                        "Config_UnsupportedFormatter"));
                throw new FlagException(
                    $"[Config] Unsupported formatter ({flag.Value}) was specified.");
            }
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString(
                        "Config_FormatterChangeWarning"),
                        _config.Formatter.Selector, flag.Value
                ));
            _config.ChangeFormatter(newFormatter);
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("Config_FormatterChanged"));
        }
        // Purge current config.
        else if (flag.Key == "purge")
        {
            FlagHasNoValue(flag, Name);
            // Require confirmation from user.
            _formatter.DisplayWarning(Name,
                _resources.GetLocalizedString("Config_PurgeWarning"));
            // Get user's confirmation.
            if (!UserConfirm()) return;
            // Purge.
            _config.PurgeConfig();
            _formatter.DisplayInfo(Name,
                _resources.GetLocalizedString("Config_Purged"));
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override void Action()
    {
        // If no flags display config state.
        if (Flags.Count == 0)
        {
            _formatter.DisplayInfo(Name,
            _resources.GetLocalizedString("Config_ListConfig"));
            _formatter.DisplayMessage(string.Format(
                _resources.GetLocalizedString("Config_Formatter"),
                _config.Formatter.Selector
            ));
            _formatter.DisplayMessage(string.Format(
                _resources.GetLocalizedString("Config_Locale"),
                _config.Locale
            ));
        }
    }
}
