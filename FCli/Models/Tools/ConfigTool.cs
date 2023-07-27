// Vendor namespaces.
using System.Resources;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Config;
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

        Description = _resources.GetString("Config_Help") 
            ?? formatter.StringNotLoaded();
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
                _formatter.DisplayError(Name, string.Format(
                    _resources.GetString("FCli_UnexpectedArg")
                    ?? _formatter.StringNotLoaded(), 
                    Name));
                throw new ArgumentException("Config attempt to call with arg");
            }
            // If no flags display config state.
            if (!flags.Any())
            {
                _formatter.DisplayInfo(Name, 
                _resources.GetString("Config_ListConfig"));
                // Temporary hardcode.
                _formatter.DisplayMessage(string.Format(
                    _resources.GetString("Config_Formatter")
                    ?? _formatter.StringNotLoaded(),
                    _config.Formatter.Selector
                ));
                _formatter.DisplayMessage(string.Format(
                    _resources.GetString("Config_Locale")
                    ?? _formatter.StringNotLoaded(),
                    _config.Locale
                ));
            }
            foreach (var flag in flags)
            {
                if (flag.Key == "locale")
                {
                    FlagHasValue(flag, Name);
                    // Guard against unsupported locale.
                    if (!_config.KnownLocales.Contains(flag.Value))
                    {
                        _formatter.DisplayError(Name, 
                            _resources.GetString("Config_UnknownLocale"));
                        throw new FlagException(
                            $"Unsupported locale ({flag.Value}) was specified.");
                    }
                    _formatter.DisplayWarning(
                        Name,
                        string.Format(
                            _resources.GetString("Config_LocaleChangeWarning")
                            ?? _formatter.StringNotLoaded(),
                            _config.Locale, flag.Value
                        ));
                    _config.ChangeLocale(flag.Value);
                    _formatter.DisplayMessage(
                        _resources.GetString("Config_LocaleChanged"));
                }
                else if (flag.Key == "formatter")
                {
                    FlagHasValue(flag, Name);
                    var newFormatter = _config.KnownFormatters
                        .FirstOrDefault(format => format.Selector == flag.Value);
                    // Guard against unsupported locale.
                    if (newFormatter == null)
                    {
                        _formatter.DisplayError(Name, 
                            _resources.GetString("Config_UnsupportedFormatter"));
                        throw new FlagException(
                            $"Unsupported formatter ({flag.Value}) was specified.");
                    }
                    _formatter.DisplayWarning(
                        Name,
                        string.Format(
                            _resources.GetString("Config_FormatterChangeWarning")
                            ?? _formatter.StringNotLoaded(),
                            _config.Formatter.Selector, flag.Value
                        ));
                    _config.ChangeFormatter(newFormatter);
                    _formatter.DisplayMessage(
                        _resources.GetString("Config_FormatterChanged"));
                }
                else if (flag.Key == "purge")
                {
                    FlagHasNoValue(flag, Name);
                    // Require confirmation from user.
                    _formatter.DisplayWarning(Name, 
                        _resources.GetString("Config_PurgeWarning"));
                    // Get user's confirmation.
                    if (!UserConfirm()) return;
                    // Purge.
                    _config.PurgeConfig();
                    _formatter.DisplayInfo(Name,
                        _resources.GetString("Config_Purged"));
                }
                else UnknownFlag(flag, Name);
            }
        };
}