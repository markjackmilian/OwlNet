namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Provides encryption and decryption services for sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Attempts to decrypt the specified cipher text.
    /// </summary>
    /// <param name="cipherText">The encrypted text to decrypt.</param>
    /// <param name="plainText">When this method returns, contains the decrypted text if decryption succeeded, or null if it failed.</param>
    /// <returns>true if decryption succeeded; false if the cipher text could not be decrypted (e.g., protection key changed).</returns>
    bool TryDecrypt(string cipherText, out string? plainText);
}
