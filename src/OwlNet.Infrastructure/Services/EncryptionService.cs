using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using OwlNet.Application.Common.Interfaces;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IEncryptionService"/> that uses
/// ASP.NET Core Data Protection to encrypt and decrypt sensitive data such as API keys.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private const string ProtectorPurpose = "OwlNet.ApiKeyProtection";

    private readonly IDataProtector _protector;
    private readonly ILogger<EncryptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider used to create a purpose-specific protector.</param>
    /// <param name="logger">The logger instance for structured diagnostic output.</param>
    public EncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<EncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        return _protector.Protect(plainText);
    }

    /// <inheritdoc />
    public bool TryDecrypt(string cipherText, out string? plainText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            plainText = string.Empty;
            return true;
        }

        try
        {
            plainText = _protector.Unprotect(cipherText);
            return true;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to decrypt cipher text — the data protection key may have changed");

            plainText = null;
            return false;
        }
    }
}
