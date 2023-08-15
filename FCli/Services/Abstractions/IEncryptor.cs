namespace FCli.Services.Abstractions;

/// <summary>
/// Uses certain encryption algorithm to translate sensitive data.
/// </summary>
public interface IEncryptor
{
    /// <summary>
    /// Encrypts given string.
    /// </summary>
    /// <param name="unencrypted">String to encrypt.</param>
    /// <param name="passphrase">Passphrase used for encryption.</param>
    /// <returns>Encrypted string.</returns>
    public string Encrypt(string unencrypted, string passphrase);
    
    /// <summary>
    /// Decrypts given string.
    /// </summary>
    /// <param name="encrypted">String do decrypt.</param>
    /// <param name="passphrase">Passphrase used for decryption.</param>
    /// <returns>Decrypted string.</returns>
    public string Decrypt(string encrypted, string passphrase);
}
