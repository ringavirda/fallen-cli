// Vendor namespaces.
using System.Security.Cryptography;
using System.Text;
// FCli namespaces.
using FCli.Services.Abstractions;

namespace FCli.Services.Encryption;

/// <summary>
/// This class uses AES algorithm to encrypt and decrypt strings.
/// </summary>
public class AesEncryptor : IEncryptor
{
    // DI.
    private readonly IConfig _config;

    public AesEncryptor(IConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Transforms given plain text into encrypted base64 string.
    /// </summary>
    /// <param name="unencrypted">Source data.</param>
    /// <param name="passphrase">Passphrase to encrypt.</param>
    /// <returns>Encrypted base64 string.</returns>
    public string Encrypt(string unencrypted, string passphrase)
    {
        byte[] encrypted;
        using (var aes = Aes.Create())
        {
            aes.Key = ConvertPassphrase(passphrase);
            aes.IV = _config.Salt;
            aes.Padding = PaddingMode.PKCS7;
            // Create the streams used for encryption.
            using (var memory = new MemoryStream())
            {
                using (var crypto = new CryptoStream(
                    memory,
                    aes.CreateEncryptor(aes.Key, aes.IV),
                    CryptoStreamMode.Write))
                {
                    using (var writer = new StreamWriter(crypto))
                    {
                        //Write all data to the stream.
                        writer.Write(unencrypted);
                    }
                    encrypted = memory.ToArray();
                }
            }
        }
        // Return encrypted data.
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Converts given encrypted base64 string and decrypts it.
    /// </summary>
    /// <param name="encrypted64">Original data.</param>
    /// <param name="passphrase">Passphrase to decrypt.</param>
    /// <returns>Decrypted data.</returns>
    public string Decrypt(string encrypted64, string passphrase)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = ConvertPassphrase(passphrase);
            aes.IV = _config.Salt;
            aes.Padding = PaddingMode.PKCS7;
            // Create the streams used for decryption.
            using (var memory = new MemoryStream(
                Convert.FromBase64String(encrypted64)))
            {
                using (var crypto = new CryptoStream(
                    memory,
                    aes.CreateDecryptor(aes.Key, aes.IV),
                    CryptoStreamMode.Read))
                {
                    using (var reader = new StreamReader(crypto))
                    {

                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Converts given passphrase to aligned byte array. 
    /// </summary>
    /// <param name="passphrase">String to convert.</param>
    /// <returns>Aligned byte array.</returns>
    private byte[] ConvertPassphrase(string passphrase)
    {
        var bytes = new byte[16];
        Encoding.UTF8.GetBytes(passphrase).CopyTo(bytes, 0);
        return bytes;
    }
}
