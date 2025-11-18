using System.Text;
using System.Text.Json;

using FCli.Exceptions;
using FCli.Models.Identity;
using FCli.Services.Abstractions;

namespace FCli.Services.Data.Identity;

/// <summary>
/// Extension of PlainManager that uses encryption provider to safely store identities.
/// </summary>
public class EncryptedIdentityManager : PlainIdentityManager
{
    // DI.
    private readonly ICommandLineFormatter _formatter;
    private readonly IResources _resources;
    private readonly IEncryptor _encrypter;

    // Encryption.
    private string? _passphrase;

    public EncryptedIdentityManager(
        ICommandLineFormatter formatter,
        IResources resources,
        IConfig config,
        IEncryptor encrypter)
        : base(config)
    {
        _formatter = formatter;
        _resources = resources;
        _encrypter = encrypter;

        if (File.Exists(Config.PassphraseFile))
        {
            var base64 = File.ReadAllText(Config.PassphraseFile);
            _passphrase = Encoding.UTF8.GetString(
                Convert.FromBase64String(base64)).Trim('\0');
        }
    }

    /// <summary>
    /// Assumes that storage is plain and encrypts it.
    /// </summary>
    public void EncryptStorage()
    {
        // Guard against uninitialized storage.
        if (File.Exists(Config.IdentityFilePath))
        {
            // Read and deserialize plain storage.
            var jsonPlain = File.ReadAllText(Config.IdentityFilePath);
            var storage = JsonSerializer.Deserialize<IdentityStorage>(jsonPlain);
            // Write encrypted storage.
            FlushStorage(storage);
        }
        else FlushStorage(new());
    }

    /// <summary>
    /// Assumes that storage is encrypted and transforms it to plain.
    /// </summary>
    public void DecryptStorage()
    {
        // Load decrypted storage and serialize it.
        var storage = TryLoadStorage();
        var jsonPlain = storage == null
            ? JsonSerializer.Serialize(new IdentityStorage())
            : JsonSerializer.Serialize(storage);
        // Write plain json.
        File.WriteAllText(Config.IdentityFilePath, jsonPlain);
    }

    /// <summary>
    /// Tries to load and decrypt storage.
    /// </summary>
    /// <returns>Loaded storage if exists, null otherwise.</returns>
    /// <exception cref="IdentityException">If decryption fails.</exception>
    /// <exception cref="CriticalException">If storage corrupted.</exception>
    protected override IdentityStorage? TryLoadStorage()
    {
        // Guard against no file.
        if (!File.Exists(Config.IdentityFilePath))
        {
            IdentityCashe = new();
            FlushStorage(IdentityCashe);
            return IdentityCashe;
        }
        // Read and convert from base64.
        var jsonBase64 = File.ReadAllText(Config.IdentityFilePath);
        // If nothing was read, return null.
        if (string.IsNullOrEmpty(jsonBase64)) return null;
        // Get passphrase from the buffer or the user.
        _passphrase ??= TryGetPassphrase(false);
        // Try decrypt the json.
        var jsonDecrypted = _encrypter.Decrypt(jsonBase64, _passphrase);
        // Test checksum.
        if (!jsonDecrypted.Contains(IdentityStorage.StaticCheckSum))
        {
            _formatter.DisplayError(
                "Identity",
                _resources.GetLocalizedString("Identity_DecryptFailed"));
            throw new IdentityException("[Identity] Decryption failed.");
        }
        // Store passphrase temporarily if needed.
        if (!File.Exists(Config.PassphraseFile))
        {
            // Convert to base64.
            var base64 = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(_passphrase));
            // Store in temporarily.
            Config.ChangePassphraseFile(Path.GetTempFileName());
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), Config.PassphraseFile),
                base64);
        }
        // Refresh buffer.
        try
        {
            var storage = JsonSerializer.Deserialize<IdentityStorage>(jsonDecrypted);
            return IdentityCashe = storage;
        }
        catch (JsonException ex)
        {
            throw new CriticalException(
                "[Identity] Identity storage is corrupted.", ex);
        }
    }

    /// <summary>
    /// Encrypted version of storage update.
    /// </summary>
    /// <param name="storage">Identities to store.</param>
    protected override void FlushStorage(IdentityStorage? storage)
    {
        var jsonPlain = JsonSerializer.Serialize(storage);
        var jsonEncrypted64 = _encrypter.Encrypt(
            jsonPlain,
            _passphrase ?? TryGetPassphrase(true)); ;
        File.WriteAllText(Config.IdentityFilePath, jsonEncrypted64);
        // Update cashe.
        IdentityCashe = storage;
    }

    /// <summary>
    /// Asks the user to provide the passphrase.
    /// </summary>
    /// <param name="regenerateSalt">Regenerates salt if true.</param>
    /// <returns>Provided passphrase.</returns>
    /// <exception cref="IdentityException">If bad passphrase was given.</exception>
    private string TryGetPassphrase(bool regenerateSalt)
    {
        // Ask user to input the passphrase.
        _formatter.DisplayWarning(
            "Identity",
            _resources.GetLocalizedString("Identity_EnterPassphrase"));
        var input = _formatter.ReadUserInput("Passphrase", true);
        // Validate passphrase.
        if (string.IsNullOrEmpty(input)
            || input.Length < 6
            || input.Length > 16
            || Encoding.UTF8.GetByteCount(input) > 16)
        {
            _formatter.DisplayError(
                "Identity",
                _resources.GetLocalizedString("Identity_BadPassphrase"));
            throw new IdentityException("[Identity] Invalid passphrase.");
        }
        // Parse passphrase and temporarily store it.
        else
        {
            _formatter.DisplayMessage(
                _resources.GetLocalizedString("Identity_PassphraseParsed"));
            // Regenerate salt if needed.
            if (regenerateSalt) Config.ChangeSalt();
            // Return passphrase.
            return _passphrase = input;
        }
    }
}