// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using FCli.Services.Data.Identity;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

public class ConfigTool : ToolBase
{
    // DI.
    private readonly IConfig _config;
    private readonly IEncryptor _encryptor;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public ConfigTool() : base()
    {
        _config = null!;
        _encryptor = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public ConfigTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        IEncryptor encryptor)
        : base(formatter, resources)
    {
        _config = config;
        _encryptor = encryptor;

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
            _formatter.DisplayError(
                Name, 
                string.Format(
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
                _formatter.DisplayError(
                    Name,
                    _resources.GetLocalizedString("Config_UnknownLocale"));
                throw new FlagException(
                    $"[Config] Unsupported locale ({flag.Value}) was specified.");
            }
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString(
                        "Config_LocaleChangeWarning"),
                        _config.Locale, 
                        flag.Value));
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
                _formatter.DisplayError(
                    Name,
                    _resources.GetLocalizedString("Config_UnsupportedFormatter"));
                throw new FlagException(
                    $"[Config] Unsupported formatter ({flag.Value}) was specified.");
            }
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString(
                        "Config_FormatterChangeWarning"),
                        _config.Formatter.Selector, 
                        flag.Value));
            _config.ChangeFormatter(newFormatter);
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("Config_FormatterChanged"));
        }
        // Enable or disable encryption on identity data.
        else if (flag.Key == "encrypt")
        {
            FlagHasValue(flag, Name);

            // Guard against non bool value.
            if (!bool.TryParse(flag.Value, out bool encrypt))
            {
                _formatter.DisplayError(
                    Name,
                    _resources.GetLocalizedString("Config_EncryptionBadValue"));
                throw new FlagException("[Config] Encrypt bad value");
            }
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString(
                        "Config_EncryptionChangeWarning"),
                        _config.UseEncryption, 
                        encrypt));
            _config.ChangeEncryption(encrypt);
            // Create identity managers.
            var encrypted = new EncryptedIdentityManager(
                _formatter,
                _resources,
                _config,
                _encryptor);
            // Encrypt data.
            if (encrypt)
            {
                encrypted.EncryptStorage();
                _formatter.DisplayMessage(
                    _resources.GetLocalizedString("Config_EncryptionEnabled"));
            }
            // Decrypt data.
            else
            {
                encrypted.DecryptStorage();
                _formatter.DisplayMessage(
                    _resources.GetLocalizedString("Config_EncryptionDisabled"));
            }
        }
        // Purge current config.
        else if (flag.Key == "purge")
        {
            FlagHasNoValue(flag, Name);
            // Require confirmation from user.
            _formatter.DisplayWarning(
                Name,
                _resources.GetLocalizedString("Config_PurgeWarning"));
            // Get user's confirmation.
            if (!UserConfirm()) return;
            // Purge.
            _config.PurgeConfig();
            _formatter.DisplayInfo(
                Name,
                _resources.GetLocalizedString("Config_Purged"));
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override Task ActionAsync()
    {
        // If no flags display config state.
        if (Flags.Count == 0)
        {
            _formatter.DisplayInfo(
                Name,
                _resources.GetLocalizedString("Config_ListConfig"));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Config_Formatter"),
                    _config.Formatter.Selector
            ));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Config_Locale"),
                    _config.Locale
            ));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Config_Encryption"),
                    _config.UseEncryption
            ));
        }
        // Final.
        return Task.CompletedTask;
    }
}
